using databaseFirst.Data;
using databaseFirst.DTOs;
using databaseFirst.Models;
using Microsoft.EntityFrameworkCore;

namespace databaseFirst.Services;

public class DbService : IDbService
{
    private readonly DbfirstContext _context;

    public DbService(DbfirstContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PatientDto>> GetPatientsAsync(string? search)
    {
        var patients = await _context.Patients
            .Include(p => p.Admissions).ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.Room).ThenInclude(r => r.Ward)
            .Where(p => search == null ||
                        EF.Functions.Like(p.FirstName, $"%{search}%") ||
                        EF.Functions.Like(p.LastName, $"%{search}%"))
            .AsSplitQuery()
            .ToListAsync();

        return patients.Select(p => new PatientDto(
            p.Pesel,
            p.FirstName,
            p.LastName,
            p.Age,
            p.Sex ? "Male" : "Female",
            p.Admissions.Select(a => new AdmissionDto(
                a.Id,
                a.AdmissionDate,
                a.DischargeDate,
                new WardDto(a.Ward.Id, a.Ward.Name, a.Ward.Description)
            )).ToList(),
            p.BedAssignments.Select(ba => new BedAssignmentGetDto(
                ba.Id,
                ba.From,
                ba.To,
                new BedDto(
                    ba.Bed.Id,
                    new BedTypeDto(ba.Bed.BedType.Id, ba.Bed.BedType.Name, ba.Bed.BedType.Description),
                    new RoomDto(ba.Bed.Room.Id, ba.Bed.Room.HasTv, new WardDto(ba.Bed.Room.Ward.Id, ba.Bed.Room.Ward.Name, ba.Bed.Room.Ward.Description))
                )
            )).ToList()
        ));
    }

    public async Task<BedAssignment> AssignBedAsync(string pesel, AssignBedRequestDto dto)
    {
        var patient = await _context.Patients.FindAsync(pesel);
        if (patient is null)
            throw new NotFoundException($"Patient with PESEL {pesel} not found");

        var bed = await _context.Beds
            .Include(b => b.BedType)
            .Include(b => b.Room).ThenInclude(r => r.Ward)
            .Include(b => b.BedAssignments)
            .Where(b => b.BedType.Name == dto.BedType)
            .Where(b => b.Room.Ward.Name == dto.Ward)
            .Where(b => !b.BedAssignments.Any(ba =>
                (dto.To == null || ba.From < dto.To) &&
                (ba.To == null || ba.To > dto.From)))
            .FirstOrDefaultAsync();

        if (bed is null)
            throw new NotFoundException($"No available bed of type '{dto.BedType}' in ward '{dto.Ward}' for the requested time period");

        var assignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = bed.Id,
            From = dto.From,
            To = dto.To
        };

        _context.BedAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        return assignment;
    }
}

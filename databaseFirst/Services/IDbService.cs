using databaseFirst.DTOs;
using databaseFirst.Models;

namespace databaseFirst.Services;

public interface IDbService
{
    Task<IEnumerable<PatientDto>> GetPatientsAsync(string? search);
    Task<BedAssignment> AssignBedAsync(string pesel, AssignBedRequestDto dto);
}

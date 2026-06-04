using databaseFirst.DTOs;
using databaseFirst.Services;
using Microsoft.AspNetCore.Mvc;

namespace databaseFirst.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly IDbService _dbService;
    public PatientsController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
        => Ok(await _dbService.GetPatientsAsync(search));

    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBed(string pesel, [FromBody] AssignBedRequestDto dto)
    {
        try
        {
            var result = await _dbService.AssignBedAsync(pesel, dto);
            return Created($"api/patients/{pesel}/bedassignments/{result.Id}", null);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
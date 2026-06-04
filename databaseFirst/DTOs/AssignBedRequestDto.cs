namespace databaseFirst.DTOs;

public record AssignBedRequestDto(
    DateTime From,
    DateTime? To,
    string BedType,
    string Ward);

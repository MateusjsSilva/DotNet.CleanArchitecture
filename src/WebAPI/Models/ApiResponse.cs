namespace CleanArchitecture.WebAPI.Models;

public sealed record ApiResponse<T>(T Data, string? Message = null);

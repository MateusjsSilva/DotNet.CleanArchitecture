namespace CleanArchitecture.Application.Common;

/// <summary>
/// Identifies commands that target a specific aggregate by its ID.
/// Used by validators to apply shared ID-validation rules without coupling
/// them to a concrete command type.
/// </summary>
public interface IHasId
{
    Guid Id { get; }
}

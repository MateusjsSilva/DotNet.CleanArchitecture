using System.Diagnostics;

namespace CleanArchitecture.Application.Telemetry;

public static class ApplicationActivitySource
{
    public const string Name = "CleanArchitecture.Application";

    public static readonly ActivitySource Instance = new(Name);
}

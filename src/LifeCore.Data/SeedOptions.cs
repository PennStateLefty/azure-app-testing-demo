using LifeCore.Contracts;

namespace LifeCore.Data;

public sealed class SeedOptions
{
    public bool OnStartup { get; set; } = true;
    public int AgentCount { get; set; } = SeedConventions.AgentCount;
    public int CaseCount { get; set; } = SeedConventions.CaseCount;
    public int PolicyCount { get; set; } = SeedConventions.PolicyCount;
}

using LifeCore.Contracts;

namespace LifeCore.Web.Client.Services;

/// <summary>Mock "authentication": the selected persona is sent as X-Persona on every API call.</summary>
public sealed class PersonaState
{
    public static IReadOnlyList<PersonaDto> All { get; } = [.. SeedConventions.Underwriters, .. SeedConventions.Csrs];

    public PersonaDto Current { get; private set; } = SeedConventions.Underwriters[0];

    public event Action? Changed;

    public void Set(string personaId)
    {
        var next = All.FirstOrDefault(p => p.Id == personaId);
        if (next is null || next.Id == Current.Id) return;
        Current = next;
        Changed?.Invoke();
    }
}

namespace LifeCore.Contracts;

/// <summary>Deterministic seed conventions shared by the seeder, Playwright tests and JMeter CSVs.</summary>
public static class SeedConventions
{
    public const int RandomSeed = 20260101;

    public const int AgentCount = 50;
    public const int CaseCount = 2000;
    public const int PolicyCount = 10000;

    /// <summary>Case numbers: UW100001 .. UW102000.</summary>
    public const int FirstCaseNumber = 100001;
    public static string CaseNumber(int ordinal) => $"UW{FirstCaseNumber + ordinal - 1}";

    /// <summary>Policy numbers: LC1000001 .. LC1010000.</summary>
    public const int FirstPolicyNumber = 1000001;
    public static string PolicyNumber(int ordinal) => $"LC{FirstPolicyNumber + ordinal - 1}";

    /// <summary>Underwriter personas (cases are distributed round-robin by ordinal).</summary>
    public static readonly IReadOnlyList<PersonaDto> Underwriters =
    [
        new("uw-dana", "Dana Whitfield", "Underwriter"),
        new("uw-marcus", "Marcus Lee", "Underwriter"),
        new("uw-priya", "Priya Raman", "Underwriter"),
        new("uw-tomas", "Tomás Ortega", "Underwriter"),
    ];

    public static readonly IReadOnlyList<PersonaDto> Csrs =
    [
        new("csr-jordan", "Jordan Blake", "CSR"),
        new("csr-amelia", "Amelia Chen", "CSR"),
    ];

    /// <summary>Well-known records guaranteed by the seeder (used by UI tests):
    /// UW100001 – PendingRequirements, outstanding APS, BMI &gt; 30 (out-of-bounds alert).
    /// UW100002 – InUnderwriting, all requirements received (decision can succeed).
    /// LC1000001 – In-force Universal Life, 2 primary beneficiaries 50/50, has cash value.
    /// LC1000002 – In-force Fixed Indexed Annuity with 3 fund allocations.</summary>
    public const string KnownOutOfBoundsCase = "UW100001";
    public const string KnownDecisionableCase = "UW100002";
    public const string KnownUniversalLifePolicy = "LC1000001";
    public const string KnownAnnuityPolicy = "LC1000002";
}

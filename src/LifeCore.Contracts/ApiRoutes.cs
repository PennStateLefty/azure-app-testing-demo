namespace LifeCore.Contracts;

/// <summary>Single source of truth for API routes (server mapping, UI client, docs, JMeter).</summary>
public static class ApiRoutes
{
    public const string Base = "/api/v1";

    // Screen 1
    public const string DashboardKpis = Base + "/dashboard/kpis";          // ?underwriter=
    public const string Worklist = Base + "/worklist";                     // ?view=Task|Case&status=&product=&priority=&search=&underwriter=&page=1&pageSize=25&sort=ageDays_desc

    // Screen 2
    public const string Case = Base + "/cases/{caseNumber}";
    public const string CaseRequirements = Base + "/cases/{caseNumber}/requirements";
    public const string CaseRiskAssessment = Base + "/cases/{caseNumber}/risk-assessment";
    public const string CaseSummary = Base + "/cases/{caseNumber}/summary";
    public const string CaseDecision = Base + "/cases/{caseNumber}/decision";
    public const string CaseNotes = Base + "/cases/{caseNumber}/notes";
    public const string CaseChat = Base + "/cases/{caseNumber}/chat";

    // Screen 3
    public const string Policies = Base + "/policies";                     // ?query=&page=1&pageSize=25
    public const string Policy = Base + "/policies/{policyNumber}";
    public const string PolicyCoverages = Base + "/policies/{policyNumber}/coverages";
    public const string PolicyBeneficiaries = Base + "/policies/{policyNumber}/beneficiaries"; // GET, PUT
    public const string PolicyBilling = Base + "/policies/{policyNumber}/billing";
    public const string PolicyValues = Base + "/policies/{policyNumber}/values";
    public const string PolicyTransactions = Base + "/policies/{policyNumber}/transactions";
    public const string PolicyAddress = Base + "/policies/{policyNumber}/address";        // PUT
    public const string PolicyQuote = Base + "/policies/{policyNumber}/quotes";           // POST QuoteRequest

    // Product configuration (read-only catalog)
    public const string Products = Base + "/products";

    // Misc
    public const string Personas = Base + "/personas";
    public const string AdminReset = Base + "/admin/reset";

    public const string HealthLive = "/health/live";
    public const string HealthReady = "/health/ready";

    public const string CorrelationHeader = "X-Correlation-Id";
    public const string PersonaHeader = "X-Persona";

    public static string For(string template, string key) =>
        template.Replace("{caseNumber}", Uri.EscapeDataString(key)).Replace("{policyNumber}", Uri.EscapeDataString(key));
}

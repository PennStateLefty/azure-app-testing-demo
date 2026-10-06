using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LifeCore.Contracts;

namespace LifeCore.Web.Client.Services;

/// <summary>Result of a mutating API call; carries ProblemDetails info on 4xx.</summary>
public sealed record ApiResult<T>(T? Value, HttpStatusCode StatusCode, string? ErrorTitle, string? ErrorDetail, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
    public string ErrorMessage =>
        Errors.Count > 0 ? string.Join(" ", Errors.SelectMany(e => e.Value)) : ErrorDetail ?? ErrorTitle ?? $"Request failed ({(int)StatusCode}).";
}

/// <summary>Typed HTTP client for /api/v1. All screens must go through this class (plain HTTP/JSON so JMeter can replay it).</summary>
public sealed class LifeCoreApi(HttpClient http, PersonaState persona)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ----- Screen 1 -----
    public Task<DashboardKpisDto?> GetKpisAsync(string? underwriter = null, CancellationToken ct = default) =>
        GetAsync<DashboardKpisDto>(ApiRoutes.DashboardKpis + Query(("underwriter", underwriter)), ct);

    public Task<PagedResult<WorklistItemDto>?> GetWorklistAsync(
        WorklistView view, CaseStatus? status = null, string? product = null, Priority? priority = null,
        string? search = null, string? underwriter = null, int page = 1, int pageSize = 25, string? sort = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<WorklistItemDto>>(ApiRoutes.Worklist + Query(
            ("view", view.ToString()), ("status", status?.ToString()), ("product", product), ("priority", priority?.ToString()),
            ("search", search), ("underwriter", underwriter), ("page", page.ToString()), ("pageSize", pageSize.ToString()), ("sort", sort)), ct);

    public Task<IReadOnlyList<ChatMessageDto>?> GetChatAsync(string caseNumber, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ChatMessageDto>>(ApiRoutes.For(ApiRoutes.CaseChat, caseNumber), ct);

    public Task<ApiResult<ChatMessageDto>> PostChatAsync(string caseNumber, PostChatMessageRequest request, CancellationToken ct = default) =>
        SendAsync<ChatMessageDto>(HttpMethod.Post, ApiRoutes.For(ApiRoutes.CaseChat, caseNumber), request, ct);

    // ----- Screen 2 -----
    public Task<CaseDetailDto?> GetCaseAsync(string caseNumber, CancellationToken ct = default) =>
        GetAsync<CaseDetailDto>(ApiRoutes.For(ApiRoutes.Case, caseNumber), ct);

    public Task<IReadOnlyList<RequirementDto>?> GetRequirementsAsync(string caseNumber, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<RequirementDto>>(ApiRoutes.For(ApiRoutes.CaseRequirements, caseNumber), ct);

    public Task<ApiResult<RequirementDto>> OrderRequirementAsync(string caseNumber, OrderRequirementRequest request, CancellationToken ct = default) =>
        SendAsync<RequirementDto>(HttpMethod.Post, ApiRoutes.For(ApiRoutes.CaseRequirements, caseNumber), request, ct);

    public Task<RiskAssessmentDto?> GetRiskAssessmentAsync(string caseNumber, CancellationToken ct = default) =>
        GetAsync<RiskAssessmentDto>(ApiRoutes.For(ApiRoutes.CaseRiskAssessment, caseNumber), ct);

    public Task<CaseSummaryDto?> GetCaseSummaryAsync(string caseNumber, CancellationToken ct = default) =>
        GetAsync<CaseSummaryDto>(ApiRoutes.For(ApiRoutes.CaseSummary, caseNumber), ct);

    public Task<IReadOnlyList<CaseNoteDto>?> GetNotesAsync(string caseNumber, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CaseNoteDto>>(ApiRoutes.For(ApiRoutes.CaseNotes, caseNumber), ct);

    public Task<ApiResult<CaseNoteDto>> AddNoteAsync(string caseNumber, AddCaseNoteRequest request, CancellationToken ct = default) =>
        SendAsync<CaseNoteDto>(HttpMethod.Post, ApiRoutes.For(ApiRoutes.CaseNotes, caseNumber), request, ct);

    public Task<ApiResult<DecisionResultDto>> SubmitDecisionAsync(string caseNumber, DecisionRequest request, CancellationToken ct = default) =>
        SendAsync<DecisionResultDto>(HttpMethod.Post, ApiRoutes.For(ApiRoutes.CaseDecision, caseNumber), request, ct);

    // ----- Screen 3 -----
    public Task<PagedResult<PolicySearchResultDto>?> SearchPoliciesAsync(string? query, int page = 1, int pageSize = 25, CancellationToken ct = default) =>
        GetAsync<PagedResult<PolicySearchResultDto>>(ApiRoutes.Policies + Query(("query", query), ("page", page.ToString()), ("pageSize", pageSize.ToString())), ct);

    public Task<PolicyDetailDto?> GetPolicyAsync(string policyNumber, CancellationToken ct = default) =>
        GetAsync<PolicyDetailDto>(ApiRoutes.For(ApiRoutes.Policy, policyNumber), ct);

    public Task<IReadOnlyList<CoverageDto>?> GetCoveragesAsync(string policyNumber, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CoverageDto>>(ApiRoutes.For(ApiRoutes.PolicyCoverages, policyNumber), ct);

    public Task<IReadOnlyList<BeneficiaryDto>?> GetBeneficiariesAsync(string policyNumber, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<BeneficiaryDto>>(ApiRoutes.For(ApiRoutes.PolicyBeneficiaries, policyNumber), ct);

    public Task<BillingDto?> GetBillingAsync(string policyNumber, CancellationToken ct = default) =>
        GetAsync<BillingDto>(ApiRoutes.For(ApiRoutes.PolicyBilling, policyNumber), ct);

    public Task<ValuesDto?> GetValuesAsync(string policyNumber, CancellationToken ct = default) =>
        GetAsync<ValuesDto>(ApiRoutes.For(ApiRoutes.PolicyValues, policyNumber), ct);

    public Task<IReadOnlyList<TransactionDto>?> GetTransactionsAsync(string policyNumber, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<TransactionDto>>(ApiRoutes.For(ApiRoutes.PolicyTransactions, policyNumber), ct);

    public Task<ApiResult<TransactionDto>> ChangeAddressAsync(string policyNumber, ChangeAddressRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Put, ApiRoutes.For(ApiRoutes.PolicyAddress, policyNumber), request, ct);

    public Task<ApiResult<TransactionDto>> UpdateBeneficiariesAsync(string policyNumber, UpdateBeneficiariesRequest request, CancellationToken ct = default) =>
        SendAsync<TransactionDto>(HttpMethod.Put, ApiRoutes.For(ApiRoutes.PolicyBeneficiaries, policyNumber), request, ct);

    public Task<ApiResult<QuoteResultDto>> QuoteAsync(string policyNumber, QuoteRequest request, CancellationToken ct = default) =>
        SendAsync<QuoteResultDto>(HttpMethod.Post, ApiRoutes.For(ApiRoutes.PolicyQuote, policyNumber), request, ct);

    // ----- helpers -----
    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        Decorate(req);
        using var resp = await http.SendAsync(req, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return default;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body, options: Json) };
        Decorate(req);
        using var resp = await http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode)
            return new(await resp.Content.ReadFromJsonAsync<T>(Json, ct), resp.StatusCode, null, null, new Dictionary<string, string[]>());

        string? title = null, detail = null;
        var errors = new Dictionary<string, string[]>();
        try
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemPayload>(Json, ct);
            title = problem?.Title;
            detail = problem?.Detail;
            if (problem?.Errors is not null) errors = problem.Errors;
        }
        catch (JsonException) { }
        return new(default, resp.StatusCode, title, detail, errors);
    }

    private void Decorate(HttpRequestMessage req)
    {
        req.Headers.TryAddWithoutValidation(ApiRoutes.PersonaHeader, persona.Current.Id);
        req.Headers.TryAddWithoutValidation(ApiRoutes.CorrelationHeader, Guid.NewGuid().ToString("N"));
    }

    private static string Query(params (string Key, string? Value)[] parts)
    {
        var q = string.Join("&", parts.Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));
        return q.Length == 0 ? string.Empty : "?" + q;
    }

    private sealed record ProblemPayload(string? Title, string? Detail, int? Status, Dictionary<string, string[]>? Errors);
}

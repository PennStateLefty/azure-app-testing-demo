using System.Net;
using System.Net.Http.Json;
using LifeCore.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LifeCore.UnitTests;

public sealed class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    public ApiIntegrationTests(ApiFactory factory) => _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Kpis_and_worklist_perf_modes_are_available()
    {
        var kpis = await _client.GetFromJsonAsync<DashboardKpisDto>(ApiRoutes.DashboardKpis);
        Assert.NotNull(kpis);
        var task = await _client.GetFromJsonAsync<PagedResult<WorklistItemDto>>(ApiRoutes.Worklist + "?view=Task&pageSize=5");
        var cases = await _client.GetFromJsonAsync<PagedResult<WorklistItemDto>>(ApiRoutes.Worklist + "?view=Case&pageSize=5");
        Assert.NotEmpty(task!.Items);
        Assert.NotEmpty(cases!.Items);
    }

    [Fact]
    public async Task Known_case_has_bmi_alert_and_decision_validation_then_success()
    {
        var detail = await _client.GetFromJsonAsync<CaseDetailDto>(ApiRoutes.For(ApiRoutes.Case, SeedConventions.KnownOutOfBoundsCase));
        Assert.Contains(detail!.Alerts, a => a.Field == "Bmi");
        var reject = await _client.PostAsJsonAsync(ApiRoutes.For(ApiRoutes.CaseDecision, SeedConventions.KnownOutOfBoundsCase), new DecisionRequest(RiskClass.Standard, "", null, false));
        Assert.Equal((HttpStatusCode)422, reject.StatusCode);
        var ok = await _client.PostAsJsonAsync(ApiRoutes.For(ApiRoutes.CaseDecision, SeedConventions.KnownOutOfBoundsCase), new DecisionRequest(RiskClass.Standard, "Load test override", "ok", true));
        Assert.True(ok.IsSuccessStatusCode);
        var result = await ok.Content.ReadFromJsonAsync<DecisionResultDto>();
        Assert.Equal(CaseStatus.Decisioned, result!.Status);
    }

    [Fact]
    public async Task Policy_search_beneficiary_address_quote_and_correlation_work()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Policies + "?query=LC100000&pageSize=5");
        req.Headers.Add(ApiRoutes.CorrelationHeader, "abc123");
        var resp = await _client.SendAsync(req);
        Assert.Equal("abc123", resp.Headers.GetValues(ApiRoutes.CorrelationHeader).Single());
        var policies = await resp.Content.ReadFromJsonAsync<PagedResult<PolicySearchResultDto>>();
        Assert.NotEmpty(policies!.Items);

        var bad = await _client.PutAsJsonAsync(ApiRoutes.For(ApiRoutes.PolicyBeneficiaries, SeedConventions.KnownUniversalLifePolicy), new UpdateBeneficiariesRequest([new("A", "Spouse", BeneficiaryType.Primary, 90)]));
        Assert.Equal((HttpStatusCode)422, bad.StatusCode);
        var tx = await (await _client.PutAsJsonAsync(ApiRoutes.For(ApiRoutes.PolicyAddress, SeedConventions.KnownUniversalLifePolicy), new ChangeAddressRequest(new("1 Test", null, "Austin", "TX", "78701")))).Content.ReadFromJsonAsync<TransactionDto>();
        Assert.Equal(TransactionType.AddressChange, tx!.Type);
        var quote = await (await _client.PostAsJsonAsync(ApiRoutes.For(ApiRoutes.PolicyQuote, SeedConventions.KnownUniversalLifePolicy), new QuoteRequest(QuoteKind.Loan, 1000))).Content.ReadFromJsonAsync<QuoteResultDto>();
        Assert.True(quote!.MaxAvailable > 0);
    }

    [Fact]
    public async Task Admin_reset_reseeds()
    {
        var resp = await _client.PostAsync(ApiRoutes.AdminReset, null);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ResetResultDto>();
        Assert.True(result!.Policies >= 2);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Environment.CurrentDirectory, "test-dbs", $"lifecore-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:LifeCore"] = $"Data Source={_dbPath}",
            ["Seed:AgentCount"] = "6",
            ["Seed:CaseCount"] = "20",
            ["Seed:PolicyCount"] = "30",
            ["Admin:EnableReset"] = "true",
            ["Perf:UseOptimizedQueries"] = "true"
        }));
    }
}

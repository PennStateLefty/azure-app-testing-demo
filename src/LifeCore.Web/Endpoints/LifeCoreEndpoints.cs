using System.Diagnostics;
using System.Diagnostics.Metrics;
using LifeCore.Contracts;
using LifeCore.Data;
using LifeCore.Domain.Entities;
using LifeCore.Domain.Rules;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LifeCore.Web.Endpoints;

public sealed class PerfOptions { public bool UseOptimizedQueries { get; set; } = true; }

public static class LifeCoreDiagnostics
{
    public static readonly Meter Meter = new("LifeCore");
    public static readonly Counter<long> CasesDecisioned = Meter.CreateCounter<long>("cases.decisioned");
    public static readonly Counter<long> PolicyTransactionsCreated = Meter.CreateCounter<long>("policy.transactions.created");
}

public static class LifeCoreEndpoints
{
    public static IEndpointRouteBuilder MapLifeCoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Personas, () => SeedConventions.Underwriters.Concat(SeedConventions.Csrs));
        app.MapGet(ApiRoutes.DashboardKpis, GetKpis);
        app.MapGet(ApiRoutes.Worklist, GetWorklist);
        app.MapGet(ApiRoutes.Case, GetCase);
        app.MapGet(ApiRoutes.CaseRequirements, async (string caseNumber, LifeCoreDbContext db, CancellationToken ct) =>
            await ExistsCase(db, caseNumber, ct) is false ? Results.NotFound() : Results.Ok(await db.Requirements.AsNoTracking().Where(r => r.CaseNumber == caseNumber).Select(r => new RequirementDto(r.Id, r.Type, r.Status, r.OrderedDate, r.ReceivedDate, r.Vendor)).ToListAsync(ct)));
        app.MapPost(ApiRoutes.CaseRequirements, OrderRequirement);
        app.MapGet(ApiRoutes.CaseRiskAssessment, GetRiskAssessment);
        app.MapGet(ApiRoutes.CaseSummary, GetCaseSummary);
        app.MapGet(ApiRoutes.CaseNotes, async (string caseNumber, LifeCoreDbContext db, CancellationToken ct) =>
            await ExistsCase(db, caseNumber, ct) is false ? Results.NotFound() : Results.Ok(await db.CaseNotes.AsNoTracking().Where(n => n.CaseNumber == caseNumber).OrderByDescending(n => n.CreatedAt).Select(n => new CaseNoteDto(n.Id, n.CaseNumber, n.Author, n.Kind, n.Text, n.CreatedAt)).ToListAsync(ct)));
        app.MapPost(ApiRoutes.CaseNotes, AddNote);
        app.MapPost(ApiRoutes.CaseDecision, SubmitDecision);
        app.MapGet(ApiRoutes.CaseChat, async (string caseNumber, LifeCoreDbContext db, CancellationToken ct) =>
            await ExistsCase(db, caseNumber, ct) is false ? Results.NotFound() : Results.Ok(await db.ChatMessages.AsNoTracking().Where(m => m.CaseNumber == caseNumber).OrderBy(m => m.SentAt).Select(m => new ChatMessageDto(m.Id, m.CaseNumber, m.Author, m.Text, m.SentAt)).ToListAsync(ct)));
        app.MapPost(ApiRoutes.CaseChat, PostChat);
        app.MapGet(ApiRoutes.Policies, SearchPolicies);
        app.MapGet(ApiRoutes.Policy, GetPolicy);
        app.MapGet(ApiRoutes.PolicyCoverages, async (string policyNumber, LifeCoreDbContext db, CancellationToken ct) => await ExistsPolicy(db, policyNumber, ct) is false ? Results.NotFound() : Results.Ok(await db.Coverages.AsNoTracking().Where(c => c.PolicyNumber == policyNumber).Select(c => new CoverageDto(c.Id, c.Name, c.IsRider, c.Amount, c.AnnualPremium, c.EffectiveDate, c.Status)).ToListAsync(ct)));
        app.MapGet(ApiRoutes.PolicyBeneficiaries, async (string policyNumber, LifeCoreDbContext db, CancellationToken ct) => await ExistsPolicy(db, policyNumber, ct) is false ? Results.NotFound() : Results.Ok(await db.Beneficiaries.AsNoTracking().Where(b => b.PolicyNumber == policyNumber).Select(b => new BeneficiaryDto(b.Id, b.Name, b.Relationship, b.Type, b.Percent)).ToListAsync(ct)));
        app.MapGet(ApiRoutes.PolicyBilling, GetBilling);
        app.MapGet(ApiRoutes.PolicyValues, GetValues);
        app.MapGet(ApiRoutes.PolicyTransactions, async (string policyNumber, LifeCoreDbContext db, CancellationToken ct) => await ExistsPolicy(db, policyNumber, ct) is false ? Results.NotFound() : Results.Ok(await db.PolicyTransactions.AsNoTracking().Where(t => t.PolicyNumber == policyNumber).OrderByDescending(t => t.CreatedAt).Select(t => new TransactionDto(t.Id, t.Type, t.Status, t.EffectiveDate, t.Amount, t.Description, t.CreatedAt)).ToListAsync(ct)));
        app.MapPut(ApiRoutes.PolicyAddress, ChangeAddress);
        app.MapPut(ApiRoutes.PolicyBeneficiaries, UpdateBeneficiaries);
        app.MapPost(ApiRoutes.PolicyQuote, Quote);
        app.MapGet(ApiRoutes.Products, GetProducts);
        return app;
    }

    public static IEndpointRouteBuilder MapLifeCoreAdmin(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.AdminReset, async (LifeCoreSeeder seeder, IMemoryCache cache, CancellationToken ct) =>
        {
            cache.Remove("kpis:");
            var result = await seeder.ResetAsync(ct);
            return Results.Ok(result);
        });
        return app;
    }

    private static async Task<IResult> GetProducts(LifeCoreDbContext db, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking().OrderBy(p => p.Line).ThenBy(p => p.Name).ToListAsync(ct);
        var openCases = await db.Cases.AsNoTracking()
            .Where(c => c.Status != CaseStatus.Decisioned && c.Status != CaseStatus.Withdrawn)
            .GroupBy(c => c.ProductCode).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var inForce = await db.Policies.AsNoTracking()
            .Where(p => p.Status == PolicyStatus.InForce)
            .GroupBy(p => p.ProductCode).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return Results.Ok(products.Select(p => new ProductDto(p.Code, p.Name, p.Line, p.Type, openCases.GetValueOrDefault(p.Code), inForce.GetValueOrDefault(p.Code))).ToList());
    }

    private static async Task<IResult> GetKpis(string? underwriter, LifeCoreDbContext db, IMemoryCache cache, IOptionsMonitor<PerfOptions> perf, CancellationToken ct)
    {
        var key = $"kpis:{underwriter}";
        if (perf.CurrentValue.UseOptimizedQueries && cache.TryGetValue(key, out DashboardKpisDto? cached)) return Results.Ok(cached);
        var q = db.Cases.AsQueryable();
        if (!string.IsNullOrWhiteSpace(underwriter))
        {
            var display = SeedConventions.Underwriters.FirstOrDefault(u => u.Id == underwriter)?.DisplayName ?? underwriter;
            q = q.Where(c => c.AssignedUnderwriter == display);
        }
        if (!perf.CurrentValue.UseOptimizedQueries) q = q.Include(c => c.Requirements).AsTracking(); else q = q.AsNoTracking();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var open = await q.CountAsync(c => c.Status != CaseStatus.Decisioned && c.Status != CaseStatus.Withdrawn, ct);
        var pending = await q.CountAsync(c => c.Status == CaseStatus.PendingRequirements, ct);
        var aps = await db.Requirements.AsNoTracking().Where(r => r.Type == RequirementType.Aps && r.Status == RequirementStatus.Ordered).Join(q.Select(c => c.CaseNumber), r => r.CaseNumber, c => c, (r, c) => r).CountAsync(ct);
        var cases = await q.Select(c => new { c.ReceivedDate, c.Status, c.DecisionDate }).ToListAsync(ct);
        var avg = cases.Where(c => c.Status is CaseStatus.InUnderwriting or CaseStatus.PendingRequirements).Select(c => UnderwritingRules.DaysSince(c.ReceivedDate)).DefaultIfEmpty(0).Average();
        var status = await q.GroupBy(c => c.Status).Select(g => new StatusCountDto(g.Key, g.Count())).ToListAsync(ct);
        var dto = new DashboardKpisDto(open, pending, aps, Math.Round(avg, 1), cases.Count(c => UnderwritingRules.DaysSince(c.ReceivedDate) >= 30 && c.Status != CaseStatus.Decisioned), cases.Count(c => c.DecisionDate == today), status, Weekly(cases.Select(c => c.ReceivedDate), cases.Where(c => c.DecisionDate.HasValue).Select(c => c.DecisionDate!.Value)));
        if (perf.CurrentValue.UseOptimizedQueries) cache.Set(key, dto, TimeSpan.FromSeconds(15));
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetWorklist(WorklistView view, string? status, string? product, string? priority, string? search, string? underwriter, int? page, int? pageSize, string? sort, LifeCoreDbContext db, IOptionsMonitor<PerfOptions> perf, CancellationToken ct)
    {
        var pageNumber = Math.Max(1, page ?? 1); var pageLimit = Math.Clamp(pageSize ?? 25, 1, 100);
        IQueryable<UnderwritingCase> cq = db.Cases.Include(c => c.Applicant).Include(c => c.Product).Include(c => c.Tasks);
        cq = perf.CurrentValue.UseOptimizedQueries ? cq.AsNoTracking() : cq.AsTracking();
        // Status/priority bind as strings so empty query values (status=&priority=) mean "no filter" instead of a 400.
        if (Enum.TryParse<CaseStatus>(status, true, out var statusFilter)) cq = cq.Where(c => c.Status == statusFilter);
        if (!string.IsNullOrWhiteSpace(product)) cq = cq.Where(c => c.ProductCode == product);
        if (Enum.TryParse<Priority>(priority, true, out var priorityFilter)) cq = cq.Where(c => c.Priority == priorityFilter);
        if (!string.IsNullOrWhiteSpace(underwriter))
        {
            var display = SeedConventions.Underwriters.FirstOrDefault(u => u.Id == underwriter)?.DisplayName ?? underwriter;
            cq = cq.Where(c => c.AssignedUnderwriter == display);
        }
        if (!string.IsNullOrWhiteSpace(search)) cq = cq.Where(c => c.CaseNumber.Contains(search) || c.Applicant.Name.Contains(search));
        var cases = await cq.ToListAsync(ct);
        IEnumerable<WorklistItemDto> rows = view == WorklistView.Task
            ? cases.SelectMany(c => c.Tasks.Where(t => t.Status != WorkTaskStatus.Completed).DefaultIfEmpty(), (c, t) => Row(c, t))
            : cases.Select(c => Row(c, c.Tasks.FirstOrDefault(t => t.Status != WorkTaskStatus.Completed)));
        rows = SortRows(rows, sort);
        var count = rows.Count();
        return Results.Ok(new PagedResult<WorklistItemDto>(rows.Skip((pageNumber - 1) * pageLimit).Take(pageLimit).ToList(), pageNumber, pageLimit, count));

        static WorklistItemDto Row(UnderwritingCase c, WorkTask? t) => new(c.CaseNumber, t?.Id, t?.Type, c.Applicant.Name, c.ProductCode, c.Product.Name, c.FaceAmount, t?.Priority ?? c.Priority, UnderwritingRules.DaysSince(c.ReceivedDate), c.Status, c.AssignedUnderwriter, c.Tasks.Count(x => x.Status != WorkTaskStatus.Completed));
    }

    private static IEnumerable<WorklistItemDto> SortRows(IEnumerable<WorklistItemDto> rows, string? sort) => sort switch
    {
        "faceAmount_asc" => rows.OrderBy(r => r.FaceAmount),
        "priority_desc" => rows.OrderByDescending(r => r.Priority),
        "caseNumber_asc" => rows.OrderBy(r => r.CaseNumber),
        "ageDays_desc" or null or "" => rows.OrderByDescending(r => r.AgeDays),
        _ => rows.OrderByDescending(r => r.AgeDays)
    };

    private static async Task<IResult> GetCase(string caseNumber, LifeCoreDbContext db, CancellationToken ct)
    {
        var c = await db.Cases.AsNoTracking().Include(c => c.Applicant).Include(c => c.Product).Include(c => c.Agent).Include(c => c.Requirements).FirstOrDefaultAsync(c => c.CaseNumber == caseNumber, ct);
        return c is null ? Results.NotFound() : Results.Ok(CaseDto(c));
    }

    private static async Task<IResult> OrderRequirement(string caseNumber, OrderRequirementRequest req, LifeCoreDbContext db, HttpContext http, CancellationToken ct)
    {
        var c = await db.Cases.Include(c => c.Tasks).FirstOrDefaultAsync(c => c.CaseNumber == caseNumber, ct);
        if (c is null) return Results.NotFound();
        var r = new Requirement { CaseNumber = caseNumber, Type = req.Type, Status = RequirementStatus.Ordered, OrderedDate = DateOnly.FromDateTime(DateTime.UtcNow), Vendor = req.Type == RequirementType.Aps ? "APS Connect" : "LifeCore Vendor" };
        c.Status = CaseStatus.PendingRequirements;
        c.Requirements.Add(r);
        c.Tasks.Add(new WorkTask { CaseNumber = caseNumber, Type = req.Type == RequirementType.Labs ? TaskType.ReviewLabs : TaskType.ReviewAps, Priority = c.Priority, DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), AssignedTo = c.AssignedUnderwriter, Status = WorkTaskStatus.Open });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new RequirementDto(r.Id, r.Type, r.Status, r.OrderedDate, r.ReceivedDate, r.Vendor));
    }

    private static async Task<IResult> GetRiskAssessment(string caseNumber, LifeCoreDbContext db, CancellationToken ct)
    {
        var c = await db.Cases.AsNoTracking().Include(c => c.Applicant).Include(c => c.Requirements).FirstOrDefaultAsync(c => c.CaseNumber == caseNumber, ct);
        return c is null ? Results.NotFound() : Results.Ok(UnderwritingRules.Assess(c));
    }

    private static async Task<IResult> GetCaseSummary(string caseNumber, LifeCoreDbContext db, CancellationToken ct)
    {
        var c = await db.Cases.AsNoTracking().Include(c => c.Applicant).Include(c => c.Requirements).FirstOrDefaultAsync(c => c.CaseNumber == caseNumber, ct);
        return c is null ? Results.NotFound() : Results.Ok(new CaseSummaryDto(UnderwritingRules.Summary(c), UnderwritingRules.GetAlerts(c).Select(a => a.Message).ToList()));
    }

    private static async Task<IResult> AddNote(string caseNumber, AddCaseNoteRequest req, LifeCoreDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!await ExistsCase(db, caseNumber, ct)) return Results.NotFound();
        var note = new CaseNote { CaseNumber = caseNumber, Author = Fallback(req.Author, http), Text = req.Text, Kind = "Note", CreatedAt = DateTimeOffset.UtcNow };
        db.CaseNotes.Add(note); await db.SaveChangesAsync(ct);
        return Results.Ok(new CaseNoteDto(note.Id, note.CaseNumber, note.Author, note.Kind, note.Text, note.CreatedAt));
    }

    private static async Task<IResult> SubmitDecision(string caseNumber, DecisionRequest req, LifeCoreDbContext db, HttpContext http, CancellationToken ct)
    {
        var c = await db.Cases.Include(c => c.Applicant).Include(c => c.Requirements).Include(c => c.Tasks).FirstOrDefaultAsync(c => c.CaseNumber == caseNumber, ct);
        if (c is null) return Results.NotFound();
        var errors = UnderwritingRules.ValidateDecision(c, req);
        if (errors.Count > 0) return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
        c.FinalRiskClass = req.RiskClass; c.Status = CaseStatus.Decisioned; c.DecisionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var t in c.Tasks.Where(t => t.Status != WorkTaskStatus.Completed)) t.Status = WorkTaskStatus.Completed;
        c.Notes.Add(new CaseNote { CaseNumber = caseNumber, Author = Fallback(null, http), Kind = "Decision", Text = $"Decision {req.RiskClass}: {req.Reason}. {req.Note}".Trim(), CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        LifeCoreDiagnostics.CasesDecisioned.Add(1);
        return Results.Ok(new DecisionResultDto(c.CaseNumber, c.Status, c.FinalRiskClass!.Value));
    }

    private static async Task<IResult> PostChat(string caseNumber, PostChatMessageRequest req, LifeCoreDbContext db, HttpContext http, CancellationToken ct)
    {
        if (!await ExistsCase(db, caseNumber, ct)) return Results.NotFound();
        var msg = new ChatMessage { CaseNumber = caseNumber, Author = Fallback(req.Author, http), Text = req.Text, SentAt = DateTimeOffset.UtcNow };
        db.ChatMessages.Add(msg); await db.SaveChangesAsync(ct);
        return Results.Ok(new ChatMessageDto(msg.Id, msg.CaseNumber, msg.Author, msg.Text, msg.SentAt));
    }

    private static async Task<IResult> SearchPolicies(string? query, int? page, int? pageSize, LifeCoreDbContext db, IOptionsMonitor<PerfOptions> perf, CancellationToken ct)
    {
        var pageNumber = Math.Max(1, page ?? 1); var pageLimit = Math.Clamp(pageSize ?? 25, 1, 100); query ??= string.Empty;
        IQueryable<Policy> q = db.Policies.Include(p => p.Product).Include(p => p.Owner).Include(p => p.Insured);
        if (perf.CurrentValue.UseOptimizedQueries) q = q.AsNoTracking(); else q = q.AsTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim();
            q = perf.CurrentValue.UseOptimizedQueries
                ? q.Where(p => p.PolicyNumber.StartsWith(s) || p.Owner.Name.Contains(s) || p.Insured.Name.Contains(s) || p.Owner.SsnLast4 == s)
                : q.Where(p => (p.PolicyNumber + " " + p.Owner.Name + " " + p.Insured.Name + " " + p.Owner.SsnLast4).ToLower().Contains(s.ToLower()));
        }
        var rows = q.OrderBy(p => p.PolicyNumber).Select(p => new PolicySearchResultDto(p.PolicyNumber, p.Product.Name, p.Product.Type, p.Status, p.Owner.Name, p.Insured.Name, p.Owner.SsnLast4, p.FaceAmount, p.IssueDate));
        var count = await rows.CountAsync(ct);
        return Results.Ok(new PagedResult<PolicySearchResultDto>(await rows.Skip((pageNumber - 1) * pageLimit).Take(pageLimit).ToListAsync(ct), pageNumber, pageLimit, count));
    }

    private static async Task<IResult> GetPolicy(string policyNumber, LifeCoreDbContext db, CancellationToken ct)
    {
        var p = await db.Policies.AsNoTracking().Include(p => p.Product).Include(p => p.Owner).Include(p => p.Insured).Include(p => p.Agent).Include(p => p.Transactions).FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);
        return p is null ? Results.NotFound() : Results.Ok(PolicyDto(p));
    }

    private static async Task<IResult> GetBilling(string policyNumber, LifeCoreDbContext db, CancellationToken ct)
    {
        var p = await db.Policies.AsNoTracking().Include(p => p.Payments).FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);
        return p is null ? Results.NotFound() : Results.Ok(new BillingDto(p.BillingMode, p.ModalPremium, p.NextDueDate, p.PaymentMethod, p.Payments.OrderByDescending(x => x.Date).Select(x => new PaymentDto(x.Id, x.Date, x.Amount, x.Method, x.Status)).ToList()));
    }

    private static async Task<IResult> GetValues(string policyNumber, LifeCoreDbContext db, CancellationToken ct)
    {
        var p = await db.Policies.AsNoTracking().Include(p => p.FundAllocations).FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);
        if (p is null) return Results.NotFound();
        var quote = PolicyRules.Quote(p, QuoteKind.Loan, 0);
        return Results.Ok(new ValuesDto(p.AccountValue, p.CashSurrenderValue, PolicyRules.Quote(p, QuoteKind.Withdrawal, p.CashSurrenderValue).SurrenderCharge, p.LoanBalance, quote.MaxAvailable, p.FundAllocations.Select(f => new FundAllocationDto(f.FundName, f.AllocationPercent, f.Value)).ToList()));
    }

    private static async Task<IResult> ChangeAddress(string policyNumber, ChangeAddressRequest req, LifeCoreDbContext db, CancellationToken ct)
    {
        var p = await db.Policies.Include(p => p.Owner).FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);
        if (p is null) return Results.NotFound();
        p.Owner.Address = new Address { Line1 = req.Address.Line1, Line2 = req.Address.Line2, City = req.Address.City, State = req.Address.State, PostalCode = req.Address.PostalCode };
        var tx = Tx(policyNumber, TransactionType.AddressChange, null, $"Address changed to {req.Address.City}, {req.Address.State}");
        db.PolicyTransactions.Add(tx); await db.SaveChangesAsync(ct); LifeCoreDiagnostics.PolicyTransactionsCreated.Add(1);
        return Results.Ok(TxDto(tx));
    }

    private static async Task<IResult> UpdateBeneficiaries(string policyNumber, UpdateBeneficiariesRequest req, LifeCoreDbContext db, CancellationToken ct)
    {
        var p = await db.Policies.Include(p => p.Beneficiaries).FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);
        if (p is null) return Results.NotFound();
        var errors = PolicyRules.ValidateBeneficiaries(req.Beneficiaries);
        if (errors.Count > 0) return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
        db.Beneficiaries.RemoveRange(p.Beneficiaries);
        db.Beneficiaries.AddRange(req.Beneficiaries.Select(b => new Beneficiary { PolicyNumber = policyNumber, Name = b.Name, Relationship = b.Relationship, Type = b.Type, Percent = b.Percent }));
        var tx = Tx(policyNumber, TransactionType.BeneficiaryChange, null, "Beneficiaries updated");
        db.PolicyTransactions.Add(tx); await db.SaveChangesAsync(ct); LifeCoreDiagnostics.PolicyTransactionsCreated.Add(1);
        return Results.Ok(TxDto(tx));
    }

    private static async Task<IResult> Quote(string policyNumber, QuoteRequest req, LifeCoreDbContext db, CancellationToken ct)
    {
        var p = await db.Policies.FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);
        if (p is null) return Results.NotFound();
        var q = PolicyRules.Quote(p, req.Kind, req.Amount);
        var type = req.Kind == QuoteKind.Loan ? TransactionType.LoanQuote : TransactionType.WithdrawalQuote;
        var tx = Tx(policyNumber, type, req.Amount, $"{req.Kind} quote requested; net proceeds {q.NetProceeds:C}");
        db.PolicyTransactions.Add(tx); await db.SaveChangesAsync(ct); LifeCoreDiagnostics.PolicyTransactionsCreated.Add(1);
        return Results.Ok(new QuoteResultDto(req.Kind, req.Amount, q.MaxAvailable, q.InterestRate, q.SurrenderCharge, q.NetProceeds, TxDto(tx)));
    }

    private static CaseDetailDto CaseDto(UnderwritingCase c) => new(c.CaseNumber, c.Applicant.Name, c.Applicant.DateOfBirth, UnderwritingRules.Age(c.Applicant.DateOfBirth), c.Applicant.Gender, c.ProductCode, c.Product.Name, c.FaceAmount, c.AnnualIncome, c.HeightIn, c.WeightLb, UnderwritingRules.CalculateBmi(c.HeightIn, c.WeightLb), c.Tobacco, c.Agent.Name, c.Status, c.Priority, c.AssignedUnderwriter, c.ReceivedDate, UnderwritingRules.DaysSince(c.ReceivedDate), c.SuggestedRiskClass, c.FinalRiskClass, new(c.Requirements.Count(r => r.Status == RequirementStatus.Ordered), c.Requirements.Count(r => r.Status == RequirementStatus.Received), c.Requirements.Count(r => r.Status == RequirementStatus.Ordered), c.Requirements.Count(r => r.Status == RequirementStatus.Waived)), UnderwritingRules.GetAlerts(c));
    private static PolicyDetailDto PolicyDto(Policy p) => new(p.PolicyNumber, p.ProductCode, p.Product.Name, p.Product.Line, p.Product.Type, p.Status, p.IssueDate, PartyDto(p.Owner), PartyDto(p.Insured), p.Agent.Name, p.FaceAmount, p.AccountValue, p.CashSurrenderValue, p.Transactions.Count(t => t.Status == TransactionStatus.Pending));
    private static PartyDto PartyDto(Party p) => new(p.Id, p.Role, p.Name, p.DateOfBirth, p.Gender, p.SsnLast4, new(p.Address.Line1, p.Address.Line2, p.Address.City, p.Address.State, p.Address.PostalCode));
    private static PolicyTransaction Tx(string policyNumber, TransactionType type, decimal? amount, string desc) => new() { PolicyNumber = policyNumber, Type = type, Status = TransactionStatus.Pending, EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow), Amount = amount, Description = desc, CreatedAt = DateTimeOffset.UtcNow };
    private static TransactionDto TxDto(PolicyTransaction t) => new(t.Id, t.Type, t.Status, t.EffectiveDate, t.Amount, t.Description, t.CreatedAt);
    private static async Task<bool> ExistsCase(LifeCoreDbContext db, string caseNumber, CancellationToken ct) => await db.Cases.AnyAsync(c => c.CaseNumber == caseNumber, ct);
    private static async Task<bool> ExistsPolicy(LifeCoreDbContext db, string policyNumber, CancellationToken ct) => await db.Policies.AnyAsync(c => c.PolicyNumber == policyNumber, ct);
    private static string Fallback(string? value, HttpContext http) => !string.IsNullOrWhiteSpace(value) ? value : http.Request.Headers.TryGetValue(ApiRoutes.PersonaHeader, out var p) ? p.ToString() : "System";

    private static IReadOnlyList<WeeklyVolumeDto> Weekly(IEnumerable<DateOnly> intake, IEnumerable<DateOnly> decisions)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Enumerable.Range(0, 6).Select(i => today.AddDays(-7 * (5 - i))).Select(d => d.AddDays(-(int)d.DayOfWeek)).Select(w => new WeeklyVolumeDto(w, intake.Count(x => x >= w && x < w.AddDays(7)), decisions.Count(x => x >= w && x < w.AddDays(7)))).ToList();
    }
}

using System.Diagnostics;
using LifeCore.Contracts;
using LifeCore.Domain.Entities;
using LifeCore.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeCore.Data;

public sealed class LifeCoreSeeder(LifeCoreDbContext db, IOptions<SeedOptions> options, ILogger<LifeCoreSeeder> logger)
{
    private static readonly DateOnly Anchor = new(2026, 6, 1);
    private readonly SeedOptions _options = options.Value;

    public async Task<ResetResultDto> EnsureSeededAsync(CancellationToken ct = default)
    {
        if (await db.Policies.AnyAsync(ct))
            return new(await db.Agents.CountAsync(ct), await db.Cases.CountAsync(ct), await db.Policies.CountAsync(ct), TimeSpan.Zero);
        return await SeedAsync(ct);
    }

    public async Task<ResetResultDto> ResetAsync(CancellationToken ct = default)
    {
        await DeleteAllAsync(ct);
        return await SeedAsync(ct);
    }

    private async Task DeleteAllAsync(CancellationToken ct)
    {
        await db.PolicyTransactions.ExecuteDeleteAsync(ct);
        await db.Payments.ExecuteDeleteAsync(ct);
        await db.FundAllocations.ExecuteDeleteAsync(ct);
        await db.Beneficiaries.ExecuteDeleteAsync(ct);
        await db.Coverages.ExecuteDeleteAsync(ct);
        await db.Policies.ExecuteDeleteAsync(ct);
        await db.ChatMessages.ExecuteDeleteAsync(ct);
        await db.CaseNotes.ExecuteDeleteAsync(ct);
        await db.WorkTasks.ExecuteDeleteAsync(ct);
        await db.Requirements.ExecuteDeleteAsync(ct);
        await db.Cases.ExecuteDeleteAsync(ct);
        await db.Parties.ExecuteDeleteAsync(ct);
        await db.Agents.ExecuteDeleteAsync(ct);
        await db.Products.ExecuteDeleteAsync(ct);
    }

    private async Task<ResetResultDto> SeedAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var oldDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var rnd = new Random(SeedConventions.RandomSeed);
            var products = Products();
            await db.Products.AddRangeAsync(products, ct);
            var agents = Enumerable.Range(1, Math.Max(_options.AgentCount, 1)).Select(i => new Agent
            {
                Name = $"Agent {i:000}", LicenseState = States[i % States.Length], Status = i % 17 == 0 ? "Review" : "Active"
            }).ToList();
            await db.Agents.AddRangeAsync(agents, ct);
            await db.SaveChangesAsync(ct);

            var partyPool = new List<Party>();
            for (var i = 1; i <= Math.Max(_options.CaseCount, 2); i++)
            {
                var applicant = Party(i, "Applicant", rnd);
                partyPool.Add(applicant);
                var cn = SeedConventions.CaseNumber(i);
                var known1 = cn == SeedConventions.KnownOutOfBoundsCase;
                var known2 = cn == SeedConventions.KnownDecisionableCase;
                var reqs = Requirements(cn, i, known1, known2).ToList();
                var c = new UnderwritingCase
                {
                    CaseNumber = cn,
                    Applicant = applicant,
                    ProductCode = products[i % 3].Code,
                    AgentId = agents[i % agents.Count].Id,
                    FaceAmount = known1 ? 1_500_000 : 100_000 + (i % 20) * 50_000,
                    AnnualIncome = known1 ? 45_000 : 60_000 + (i % 10) * 10_000,
                    HeightIn = known1 ? 70 : 62 + i % 14,
                    WeightLb = known1 ? 220 : 130 + i % 100,
                    Tobacco = i % 9 == 0,
                    Status = known1 ? CaseStatus.PendingRequirements : known2 ? CaseStatus.InUnderwriting : i % 7 == 0 ? CaseStatus.PendingRequirements : CaseStatus.InUnderwriting,
                    AssignedUnderwriter = SeedConventions.Underwriters[(i - 1) % SeedConventions.Underwriters.Count].DisplayName,
                    ReceivedDate = Anchor.AddDays(-(i % 45 + 1)),
                    Priority = i % 23 == 0 ? Priority.Rush : i % 5 == 0 ? Priority.High : Priority.Normal,
                    Requirements = reqs,
                    Tasks = Tasks(cn, i).ToList(),
                    Notes = [new CaseNote { CaseNumber = cn, Author = "System", Kind = "Note", Text = "Case created", CreatedAt = new DateTimeOffset(Anchor.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) }],
                    ChatMessages = i % 25 == 0 ? [new ChatMessage { CaseNumber = cn, Author = "Dana Whitfield", Text = "Please review latest APS when received.", SentAt = DateTimeOffset.UtcNow.AddDays(-2) }] : []
                };
                c.SuggestedRiskClass = UnderwritingRules.Assess(c).SuggestedRiskClass;
                await db.Cases.AddAsync(c, ct);
                if (i % 250 == 0) { await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); logger.LogInformation("Seeded {Count} cases", i); }
            }
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            var policyCount = Math.Max(_options.PolicyCount, 2);
            for (var i = 1; i <= policyCount; i++)
            {
                var knownUl = i == 1;
                var knownAnnuity = i == 2;
                var owner = Party(100_000 + i, "Owner", rnd);
                if (knownUl) { owner.Name = "Morgan Avery"; owner.SsnLast4 = "0001"; }
                var insured = Party(200_000 + i, "Insured", rnd);
                if (knownAnnuity) { owner.Name = "Taylor Quinn"; owner.SsnLast4 = "0002"; }
                var product = knownAnnuity ? products.Single(p => p.Code == "FIA") : knownUl ? products.Single(p => p.Code == "UL") : products[i % products.Count];
                var pn = SeedConventions.PolicyNumber(i);
                var cash = product.Line == ProductLine.Annuity || product.Type is ProductType.UniversalLife or ProductType.IndexedUniversalLife or ProductType.VariableAnnuity ? 25_000 + i % 200 * 500 : 0;
                var p = new Policy
                {
                    PolicyNumber = pn,
                    ProductCode = product.Code,
                    Owner = owner,
                    Insured = insured,
                    AgentId = agents[i % agents.Count].Id,
                    Status = i % 31 == 0 ? PolicyStatus.Lapsed : PolicyStatus.InForce,
                    IssueDate = Anchor.AddYears(-(i % 12 + 1)).AddDays(i % 28),
                    FaceAmount = product.Line == ProductLine.Life ? 100_000 + i % 25 * 25_000 : 0,
                    AccountValue = cash + 5_000,
                    CashSurrenderValue = cash,
                    LoanBalance = i % 10 == 0 ? 1_000 : 0,
                    BillingMode = product.Line == ProductLine.Annuity ? BillingMode.SinglePremium : BillingMode.Monthly,
                    ModalPremium = product.Line == ProductLine.Annuity ? 0 : 125 + i % 200,
                    NextDueDate = Anchor.AddMonths(i % 12),
                    PaymentMethod = "ACH"
                };
                p.Coverages.Add(new Coverage { PolicyNumber = pn, Name = product.Name, IsRider = false, Amount = Math.Max(p.FaceAmount, p.AccountValue), AnnualPremium = p.ModalPremium * 12, EffectiveDate = p.IssueDate });
                if (product.Line == ProductLine.Life && i % 3 == 0) p.Coverages.Add(new Coverage { PolicyNumber = pn, Name = "Waiver of Premium", IsRider = true, Amount = 0, AnnualPremium = 60, EffectiveDate = p.IssueDate });
                p.Beneficiaries.AddRange(knownUl ? [
                    new Beneficiary { PolicyNumber = pn, Name = "Alex Avery", Relationship = "Spouse", Type = BeneficiaryType.Primary, Percent = 50 },
                    new Beneficiary { PolicyNumber = pn, Name = "Riley Avery", Relationship = "Child", Type = BeneficiaryType.Primary, Percent = 50 }]
                    : [new Beneficiary { PolicyNumber = pn, Name = $"Beneficiary {i}", Relationship = "Spouse", Type = BeneficiaryType.Primary, Percent = 100 }]);
                if (knownAnnuity || product.Line == ProductLine.Annuity || product.Type is ProductType.IndexedUniversalLife or ProductType.VariableAnnuity)
                    p.FundAllocations.AddRange(Funds(pn, p.AccountValue, knownAnnuity));
                for (var m = 0; m < 12 && p.ModalPremium > 0; m++)
                    p.Payments.Add(new Payment { PolicyNumber = pn, Date = Anchor.AddMonths(-m), Amount = p.ModalPremium, Method = "ACH", Status = "Posted" });
                p.Transactions.Add(new PolicyTransaction { PolicyNumber = pn, Type = TransactionType.Issue, Status = TransactionStatus.Completed, EffectiveDate = p.IssueDate, Amount = p.FaceAmount, Description = "Policy issued", CreatedAt = new DateTimeOffset(p.IssueDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) });
                await db.Policies.AddAsync(p, ct);
                if (i % 500 == 0) { await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); logger.LogInformation("Seeded {Count} policies", i); }
            }
            await db.SaveChangesAsync(ct);
            sw.Stop();
            return new(await db.Agents.CountAsync(ct), await db.Cases.CountAsync(ct), await db.Policies.CountAsync(ct), sw.Elapsed);
        }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = oldDetect; }
    }

    private static List<Product> Products() =>
    [
        new() { Code = "TERM", Name = "Term Life 20", Line = ProductLine.Life, Type = ProductType.Term },
        new() { Code = "UL", Name = "Universal Life", Line = ProductLine.Life, Type = ProductType.UniversalLife },
        new() { Code = "IUL", Name = "Indexed Universal Life", Line = ProductLine.Life, Type = ProductType.IndexedUniversalLife },
        new() { Code = "FIA", Name = "Fixed Indexed Annuity", Line = ProductLine.Annuity, Type = ProductType.FixedIndexedAnnuity },
        new() { Code = "VA", Name = "Variable Annuity", Line = ProductLine.Annuity, Type = ProductType.VariableAnnuity }
    ];

    private static IEnumerable<Requirement> Requirements(string cn, int i, bool known1, bool known2)
    {
        var types = new[] { RequirementType.Aps, RequirementType.Labs, RequirementType.Rx, RequirementType.Mvr };
        foreach (var (t, idx) in types.Select((t, idx) => (t, idx)))
        {
            var ordered = Anchor.AddDays(-(i % 20 + idx));
            var status = known2 ? RequirementStatus.Received : known1 && t == RequirementType.Aps ? RequirementStatus.Ordered : (i + idx) % 5 == 0 ? RequirementStatus.Ordered : RequirementStatus.Received;
            yield return new Requirement { CaseNumber = cn, Type = t, Status = status, OrderedDate = ordered, ReceivedDate = status == RequirementStatus.Received ? ordered.AddDays(2) : null, Vendor = t == RequirementType.Aps ? "APS Connect" : "LifeCore Vendor" };
        }
    }

    private static IEnumerable<WorkTask> Tasks(string cn, int i)
    {
        var types = new[] { TaskType.ReviewApplication, TaskType.ReviewAps, TaskType.OrderLabs, TaskType.FinalDecision };
        foreach (var (t, idx) in types.Select((t, idx) => (t, idx)))
            yield return new WorkTask { CaseNumber = cn, Type = t, Priority = i % 23 == 0 ? Priority.Rush : Priority.Normal, DueDate = Anchor.AddDays(i % 30 + idx), AssignedTo = SeedConventions.Underwriters[(i - 1) % SeedConventions.Underwriters.Count].DisplayName, Status = idx == 0 && i % 9 == 0 ? WorkTaskStatus.Completed : WorkTaskStatus.Open };
    }

    private static IEnumerable<FundAllocation> Funds(string pn, decimal value, bool exactThree)
    {
        var names = exactThree ? new[] { "S&P 500 Index", "Bond Index", "Declared Rate" } : new[] { "Balanced Fund", "Growth Fund", "Income Fund" };
        var percents = new[] { 50m, 30m, 20m };
        for (var i = 0; i < names.Length; i++) yield return new FundAllocation { PolicyNumber = pn, FundName = names[i], AllocationPercent = percents[i], Value = decimal.Round(value * percents[i] / 100m, 2) };
    }

    private static Party Party(int i, string role, Random rnd) => new()
    {
        Name = $"{FirstNames[i % FirstNames.Length]} {LastNames[(i * 7) % LastNames.Length]}",
        DateOfBirth = Anchor.AddYears(-(25 + i % 55)).AddDays(i % 365),
        Gender = i % 2 == 0 ? "F" : "M",
        Role = role,
        SsnLast4 = (1000 + i % 9000).ToString(),
        Address = new Address { Line1 = $"{100 + i} Main St", City = Cities[i % Cities.Length], State = States[i % States.Length], PostalCode = $"{10000 + rnd.Next(89999)}" }
    };

    private static readonly string[] FirstNames = ["Avery", "Jordan", "Morgan", "Taylor", "Casey", "Riley", "Jamie", "Parker", "Skyler", "Quinn"];
    private static readonly string[] LastNames = ["Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia", "Wilson", "Anderson"];
    private static readonly string[] Cities = ["Columbus", "Austin", "Phoenix", "Raleigh", "Denver", "Seattle"];
    private static readonly string[] States = ["OH", "TX", "AZ", "NC", "CO", "WA"];
}

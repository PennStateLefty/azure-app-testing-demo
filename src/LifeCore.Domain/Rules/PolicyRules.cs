using LifeCore.Contracts;
using LifeCore.Domain.Entities;

namespace LifeCore.Domain.Rules;

public static class PolicyRules
{
    public static Dictionary<string, string[]> ValidateBeneficiaries(IEnumerable<BeneficiaryInput> beneficiaries)
    {
        var list = beneficiaries.ToList();
        var errors = new Dictionary<string, string[]>();
        if (list.Any(b => string.IsNullOrWhiteSpace(b.Name))) errors["beneficiaries"] = ["Beneficiary names are required."];
        if (list.Any(b => b.Percent <= 0)) errors["percent"] = ["Each beneficiary percent must be greater than 0."];
        var primary = list.Where(b => b.Type == BeneficiaryType.Primary).Sum(b => b.Percent);
        var contingent = list.Where(b => b.Type == BeneficiaryType.Contingent).Sum(b => b.Percent);
        if (primary != 100) errors["primary"] = ["Primary beneficiary percentages must total 100%."];
        if (contingent is not (0 or 100)) errors["contingent"] = ["Contingent beneficiary percentages must total 0% or 100%."];
        return errors;
    }

    public static (decimal MaxAvailable, decimal InterestRate, decimal SurrenderCharge, decimal NetProceeds) Quote(Policy p, QuoteKind kind, decimal amount, DateOnly? asOf = null)
    {
        var today = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var year = Math.Max(1, today.Year - p.IssueDate.Year + 1);
        if (kind == QuoteKind.Loan)
        {
            var max = Math.Max(0, p.CashSurrenderValue * 0.90m - p.LoanBalance);
            return (decimal.Round(max, 2), 0.06m, 0m, decimal.Round(Math.Min(amount, max), 2));
        }
        var rate = year switch { <= 1 => .07m, 2 => .06m, 3 => .05m, 4 => .04m, 5 => .03m, 6 => .02m, 7 => .01m, _ => 0m };
        var charge = decimal.Round(Math.Min(amount, p.CashSurrenderValue) * rate, 2);
        return (decimal.Round(p.CashSurrenderValue, 2), 0m, charge, decimal.Round(Math.Min(amount, p.CashSurrenderValue) - charge, 2));
    }
}

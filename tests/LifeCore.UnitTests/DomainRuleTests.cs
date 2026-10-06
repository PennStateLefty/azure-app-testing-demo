using LifeCore.Contracts;
using LifeCore.Domain.Entities;
using LifeCore.Domain.Rules;

namespace LifeCore.UnitTests;

public sealed class DomainRuleTests
{
    [Fact]
    public void Bmi_and_alerts_detect_out_of_bounds()
    {
        var c = Case(70, 220, 50_000, 2_000_000, false);
        Assert.Equal(31.6, UnderwritingRules.CalculateBmi(c.HeightIn, c.WeightLb), 1);
        Assert.Contains(UnderwritingRules.GetAlerts(c), a => a.Field == "Bmi" && a.Severity == AlertSeverity.Warning);
        Assert.Contains(UnderwritingRules.GetAlerts(c), a => a.Field == "FaceAmount");
    }

    [Fact]
    public void Risk_class_reflects_debits()
    {
        var c = Case(70, 250, 100_000, 100_000, true);
        c.Requirements.Add(new Requirement { Type = RequirementType.Aps, Status = RequirementStatus.Ordered });
        var assessment = UnderwritingRules.Assess(c);
        Assert.True(assessment.TotalDebits >= 150);
        Assert.True(assessment.SuggestedRiskClass >= RiskClass.Table2);
    }

    [Fact]
    public void Beneficiary_validation_enforces_percentages()
    {
        var errors = PolicyRules.ValidateBeneficiaries([new("A", "Spouse", BeneficiaryType.Primary, 60)]);
        Assert.Contains("primary", errors.Keys);
        Assert.Empty(PolicyRules.ValidateBeneficiaries([new("A", "Spouse", BeneficiaryType.Primary, 50), new("B", "Child", BeneficiaryType.Primary, 50)]));
    }

    [Fact]
    public void Quote_formulas_calculate_loan_and_withdrawal()
    {
        var p = new Policy { IssueDate = new DateOnly(2021, 1, 1), CashSurrenderValue = 10_000, LoanBalance = 1_000 };
        var loan = PolicyRules.Quote(p, QuoteKind.Loan, 2_000, new DateOnly(2026, 1, 1));
        Assert.Equal(8_000, loan.MaxAvailable);
        Assert.Equal(.06m, loan.InterestRate);
        var withdrawal = PolicyRules.Quote(p, QuoteKind.Withdrawal, 1_000, new DateOnly(2026, 1, 1));
        Assert.Equal(20, withdrawal.SurrenderCharge);
        Assert.Equal(980, withdrawal.NetProceeds);
    }

    [Fact]
    public void Decision_validation_requires_reason_and_override()
    {
        var c = Case(70, 180, 100_000, 100_000, false);
        c.Requirements.Add(new Requirement { Type = RequirementType.Aps, Status = RequirementStatus.Ordered });
        var errors = UnderwritingRules.ValidateDecision(c, new(RiskClass.Standard, "", null, false));
        Assert.Contains("reason", errors.Keys);
        Assert.Contains("overrideOutstandingRequirements", errors.Keys);
    }

    private static UnderwritingCase Case(int h, int w, decimal income, decimal face, bool tobacco) => new()
    {
        Applicant = new Party { DateOfBirth = new DateOnly(1985, 1, 1), Name = "Test Applicant" },
        Product = new Product { Code = "UL", Name = "Universal Life" },
        HeightIn = h,
        WeightLb = w,
        AnnualIncome = income,
        FaceAmount = face,
        Tobacco = tobacco
    };
}

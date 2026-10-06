using LifeCore.Contracts;
using LifeCore.Domain.Entities;

namespace LifeCore.Domain.Rules;

public static class UnderwritingRules
{
    public static double CalculateBmi(int heightIn, int weightLb) => heightIn <= 0 ? 0 : Math.Round(weightLb * 703d / (heightIn * heightIn), 1);

    public static IReadOnlyList<OutOfBoundsAlertDto> GetAlerts(UnderwritingCase c)
    {
        var alerts = new List<OutOfBoundsAlertDto>();
        var bmi = CalculateBmi(c.HeightIn, c.WeightLb);
        if (bmi > 35) alerts.Add(new("Bmi", $"BMI {bmi:N1} exceeds automated underwriting limit.", AlertSeverity.Error));
        else if (bmi > 30 || bmi < 18.5) alerts.Add(new("Bmi", $"BMI {bmi:N1} outside preferred range.", AlertSeverity.Warning));
        var age = Age(c.Applicant.DateOfBirth);
        var multiple = age < 40 ? 30 : age < 60 ? 20 : 10;
        if (c.AnnualIncome > 0 && c.FaceAmount > c.AnnualIncome * multiple)
            alerts.Add(new("FaceAmount", $"Face amount exceeds {multiple}x annual income guideline.", AlertSeverity.Warning));
        if (c.Tobacco) alerts.Add(new("Tobacco", "Tobacco use reported.", AlertSeverity.Info));
        return alerts;
    }

    public static RiskAssessmentDto Assess(UnderwritingCase c)
    {
        var bmi = CalculateBmi(c.HeightIn, c.WeightLb);
        var factors = new List<RiskFactorDto>();
        var debits = 0;
        void Add(string cat, string desc, int d) { debits += d; factors.Add(new(cat, desc, d)); }
        if (bmi is > 35 or < 17) Add("Build", $"BMI {bmi:N1} outside table limits", 100);
        else if (bmi is > 30 or < 18.5) Add("Build", $"BMI {bmi:N1} outside preferred range", 50);
        var age = Age(c.Applicant.DateOfBirth);
        if (age >= 70) Add("Age", "Age 70+", 50); else if (age >= 60) Add("Age", "Age 60+", 25);
        if (c.Tobacco) Add("Tobacco", "Tobacco use", 75);
        var outstanding = c.Requirements.Count(r => r.Status == RequirementStatus.Ordered && r.Type is RequirementType.Aps or RequirementType.Labs);
        if (outstanding > 0) Add("Requirements", $"{outstanding} APS/Labs outstanding", 25 * outstanding);
        var risk = debits switch
        {
            >= 200 => RiskClass.Table4,
            >= 150 => RiskClass.Table2,
            >= 100 => RiskClass.Standard,
            >= 50 => RiskClass.StandardPlus,
            >= 25 => RiskClass.Preferred,
            _ => RiskClass.PreferredPlus
        };
        return new(bmi, bmi < 18.5 ? "Underweight" : bmi <= 30 ? "Preferred" : bmi <= 35 ? "Rated" : "Decline", debits, risk, factors);
    }

    public static Dictionary<string, string[]> ValidateDecision(UnderwritingCase c, DecisionRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Reason)) errors["reason"] = ["Decision reason is required."];
        if (!request.OverrideOutstandingRequirements && c.Requirements.Any(r => r.Status == RequirementStatus.Ordered && r.Type is RequirementType.Aps or RequirementType.Labs))
            errors["overrideOutstandingRequirements"] = ["APS/Labs requirements are outstanding. Override is required to decision this case."];
        return errors;
    }

    public static string Summary(UnderwritingCase c)
    {
        var received = c.Requirements.Count(r => r.Status == RequirementStatus.Received);
        var total = c.Requirements.Count;
        var bmi = CalculateBmi(c.HeightIn, c.WeightLb);
        var risk = Assess(c).SuggestedRiskClass;
        var bmiText = bmi is > 30 or < 18.5 ? $" BMI {bmi:N1} outside preferred range;" : string.Empty;
        return $"{received} of {total} requirements received;{bmiText} suggested {risk}.".Replace("; suggested", "; suggested");
    }

    public static int Age(DateOnly dob, DateOnly? asOf = null)
    {
        var d = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = d.Year - dob.Year;
        if (dob > d.AddYears(-age)) age--;
        return age;
    }

    public static int DaysSince(DateOnly date) => Math.Max(0, DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - date.DayNumber);
}

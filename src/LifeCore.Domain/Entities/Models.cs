using LifeCore.Contracts;

namespace LifeCore.Domain.Entities;

public sealed class Party
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public string SsnLast4 { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class Address
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public sealed class Agent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LicenseState { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

public sealed class Product
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductLine Line { get; set; }
    public ProductType Type { get; set; }
}

public sealed class UnderwritingCase
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public int ApplicantId { get; set; }
    public Party Applicant { get; set; } = null!;
    public string ProductCode { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
    public int AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    public decimal FaceAmount { get; set; }
    public decimal AnnualIncome { get; set; }
    public int HeightIn { get; set; }
    public int WeightLb { get; set; }
    public bool Tobacco { get; set; }
    public CaseStatus Status { get; set; }
    public string AssignedUnderwriter { get; set; } = string.Empty;
    public DateOnly ReceivedDate { get; set; }
    public Priority Priority { get; set; }
    public RiskClass? SuggestedRiskClass { get; set; }
    public RiskClass? FinalRiskClass { get; set; }
    public DateOnly? DecisionDate { get; set; }
    public List<Requirement> Requirements { get; set; } = [];
    public List<WorkTask> Tasks { get; set; } = [];
    public List<CaseNote> Notes { get; set; } = [];
    public List<ChatMessage> ChatMessages { get; set; } = [];
}

public sealed class Requirement
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public UnderwritingCase Case { get; set; } = null!;
    public RequirementType Type { get; set; }
    public RequirementStatus Status { get; set; }
    public DateOnly OrderedDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public string Vendor { get; set; } = "LifeCore Vendor";
}

public sealed class WorkTask
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public UnderwritingCase Case { get; set; } = null!;
    public TaskType Type { get; set; }
    public Priority Priority { get; set; }
    public DateOnly DueDate { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public WorkTaskStatus Status { get; set; }
}

public sealed class CaseNote
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public UnderwritingCase Case { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public string Kind { get; set; } = "Note";
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ChatMessage
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public UnderwritingCase Case { get; set; } = null!;
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
}

public sealed class Policy
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
    public PolicyStatus Status { get; set; }
    public DateOnly IssueDate { get; set; }
    public int OwnerId { get; set; }
    public Party Owner { get; set; } = null!;
    public int InsuredId { get; set; }
    public Party Insured { get; set; } = null!;
    public int AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    public decimal FaceAmount { get; set; }
    public decimal AccountValue { get; set; }
    public decimal CashSurrenderValue { get; set; }
    public decimal LoanBalance { get; set; }
    public BillingMode BillingMode { get; set; }
    public decimal ModalPremium { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public string PaymentMethod { get; set; } = "ACH";
    public List<Coverage> Coverages { get; set; } = [];
    public List<Beneficiary> Beneficiaries { get; set; } = [];
    public List<FundAllocation> FundAllocations { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<PolicyTransaction> Transactions { get; set; } = [];
}

public sealed class Coverage
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Policy Policy { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsRider { get; set; }
    public decimal Amount { get; set; }
    public decimal AnnualPremium { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class Beneficiary
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Policy Policy { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public BeneficiaryType Type { get; set; }
    public decimal Percent { get; set; }
}

public sealed class FundAllocation
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Policy Policy { get; set; } = null!;
    public string FundName { get; set; } = string.Empty;
    public decimal AllocationPercent { get; set; }
    public decimal Value { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Policy Policy { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "ACH";
    public string Status { get; set; } = "Posted";
}

public sealed class PolicyTransaction
{
    public int Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Policy Policy { get; set; } = null!;
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public decimal? Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

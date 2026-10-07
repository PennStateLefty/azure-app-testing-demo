namespace LifeCore.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// ---------- Product configuration (read-only) ----------

public sealed record ProductDto(
    string Code,
    string Name,
    ProductLine Line,
    ProductType Type,
    int OpenCases,
    int InForcePolicies);

// ---------- Screen 1: Underwriting Case Dashboard ----------

public sealed record DashboardKpisDto(
    int OpenCases,
    int CasesPendingRequirements,
    int ApsPending,
    double AvgDaysInUnderwriting,
    int CasesAtSlaRisk,
    int DecisionsToday,
    IReadOnlyList<StatusCountDto> CasesByStatus,
    IReadOnlyList<WeeklyVolumeDto> WeeklyIntake);

public sealed record StatusCountDto(CaseStatus Status, int Count);

public sealed record WeeklyVolumeDto(DateOnly WeekStart, int Intake, int Decisions);

/// <summary>One row in the work queue. In <see cref="WorklistView.Task"/> view each row is a task;
/// in <see cref="WorklistView.Case"/> view each row is a case (TaskId/TaskType = first open task).</summary>
public sealed record WorklistItemDto(
    string CaseNumber,
    int? TaskId,
    TaskType? TaskType,
    string Applicant,
    string ProductCode,
    string ProductName,
    decimal FaceAmount,
    Priority Priority,
    int AgeDays,
    CaseStatus Status,
    string AssignedTo,
    int OpenTaskCount);

public sealed record ChatMessageDto(int Id, string CaseNumber, string Author, string Text, DateTimeOffset SentAt);

public sealed record PostChatMessageRequest(string Author, string Text);

// ---------- Screen 2: Case Workbench ----------

public sealed record CaseDetailDto(
    string CaseNumber,
    string ApplicantName,
    DateOnly DateOfBirth,
    int Age,
    string Gender,
    string ProductCode,
    string ProductName,
    decimal FaceAmount,
    decimal AnnualIncome,
    int HeightIn,
    int WeightLb,
    double Bmi,
    bool Tobacco,
    string AgentName,
    CaseStatus Status,
    Priority Priority,
    string AssignedUnderwriter,
    DateOnly ReceivedDate,
    int DaysOpen,
    RiskClass? SuggestedRiskClass,
    RiskClass? FinalRiskClass,
    RequirementCountsDto RequirementCounts,
    IReadOnlyList<OutOfBoundsAlertDto> Alerts);

public sealed record RequirementCountsDto(int Ordered, int Received, int Outstanding, int Waived);

/// <summary>Field = the application field that is out of bounds (e.g. "Bmi", "FaceAmount").</summary>
public sealed record OutOfBoundsAlertDto(string Field, string Message, AlertSeverity Severity);

public sealed record RequirementDto(
    int Id,
    RequirementType Type,
    RequirementStatus Status,
    DateOnly OrderedDate,
    DateOnly? ReceivedDate,
    string Vendor);

public sealed record OrderRequirementRequest(RequirementType Type);

public sealed record RiskAssessmentDto(
    double Bmi,
    string BuildRating,
    int TotalDebits,
    RiskClass SuggestedRiskClass,
    IReadOnlyList<RiskFactorDto> Factors);

public sealed record RiskFactorDto(string Category, string Description, int Debits);

public sealed record CaseSummaryDto(string Summary, IReadOnlyList<string> Highlights);

public sealed record CaseNoteDto(int Id, string CaseNumber, string Author, string Kind, string Text, DateTimeOffset CreatedAt);

public sealed record AddCaseNoteRequest(string Author, string Text);

/// <summary>Rejected with 422 (ValidationProblemDetails) when required requirements are
/// outstanding and <see cref="OverrideOutstandingRequirements"/> is false, or when Reason is empty.</summary>
public sealed record DecisionRequest(RiskClass RiskClass, string Reason, string? Note, bool OverrideOutstandingRequirements);

public sealed record DecisionResultDto(string CaseNumber, CaseStatus Status, RiskClass FinalRiskClass);

// ---------- Screen 3: Policy 360 ----------

public sealed record PolicySearchResultDto(
    string PolicyNumber,
    string ProductName,
    ProductType ProductType,
    PolicyStatus Status,
    string OwnerName,
    string InsuredName,
    string OwnerSsnLast4,
    decimal FaceAmount,
    DateOnly IssueDate);

public sealed record AddressDto(string Line1, string? Line2, string City, string State, string PostalCode);

public sealed record PartyDto(int Id, string Role, string Name, DateOnly DateOfBirth, string Gender, string SsnLast4, AddressDto Address);

public sealed record PolicyDetailDto(
    string PolicyNumber,
    string ProductCode,
    string ProductName,
    ProductLine ProductLine,
    ProductType ProductType,
    PolicyStatus Status,
    DateOnly IssueDate,
    PartyDto Owner,
    PartyDto Insured,
    string AgentName,
    decimal FaceAmount,
    decimal AccountValue,
    decimal CashSurrenderValue,
    int PendingTransactionCount);

public sealed record CoverageDto(int Id, string Name, bool IsRider, decimal Amount, decimal AnnualPremium, DateOnly EffectiveDate, string Status);

public sealed record BeneficiaryDto(int Id, string Name, string Relationship, BeneficiaryType Type, decimal Percent);

public sealed record BillingDto(
    BillingMode Mode,
    decimal ModalPremium,
    DateOnly? NextDueDate,
    string PaymentMethod,
    IReadOnlyList<PaymentDto> Payments);

public sealed record PaymentDto(int Id, DateOnly Date, decimal Amount, string Method, string Status);

public sealed record ValuesDto(
    decimal AccountValue,
    decimal CashSurrenderValue,
    decimal SurrenderCharge,
    decimal LoanBalance,
    decimal MaxLoanAvailable,
    IReadOnlyList<FundAllocationDto> Funds);

public sealed record FundAllocationDto(string FundName, decimal AllocationPercent, decimal Value);

public sealed record TransactionDto(
    int Id,
    TransactionType Type,
    TransactionStatus Status,
    DateOnly EffectiveDate,
    decimal? Amount,
    string Description,
    DateTimeOffset CreatedAt);

public sealed record ChangeAddressRequest(AddressDto Address);

public sealed record BeneficiaryInput(string Name, string Relationship, BeneficiaryType Type, decimal Percent);

/// <summary>Rejected with 422 when Primary percents do not total 100, or Contingent percents are not 0 or 100.</summary>
public sealed record UpdateBeneficiariesRequest(IReadOnlyList<BeneficiaryInput> Beneficiaries);

public sealed record QuoteRequest(QuoteKind Kind, decimal Amount);

public sealed record QuoteResultDto(
    QuoteKind Kind,
    decimal RequestedAmount,
    decimal MaxAvailable,
    decimal InterestRate,
    decimal SurrenderCharge,
    decimal NetProceeds,
    TransactionDto Transaction);

// ---------- Admin / misc ----------

public sealed record ResetResultDto(int Agents, int Cases, int Policies, TimeSpan Elapsed);

public sealed record PersonaDto(string Id, string DisplayName, string Role);

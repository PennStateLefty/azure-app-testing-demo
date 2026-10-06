using System.Text.Json.Serialization;

namespace LifeCore.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<CaseStatus>))]
public enum CaseStatus { Submitted, InUnderwriting, PendingRequirements, Decisioned, Withdrawn }

[JsonConverter(typeof(JsonStringEnumConverter<Priority>))]
public enum Priority { Low, Normal, High, Rush }

[JsonConverter(typeof(JsonStringEnumConverter<TaskType>))]
public enum TaskType { ReviewApplication, ReviewAps, OrderLabs, ReviewLabs, ReviewRx, ReviewMvr, FinalDecision }

[JsonConverter(typeof(JsonStringEnumConverter<WorkTaskStatus>))]
public enum WorkTaskStatus { Open, InProgress, Completed }

[JsonConverter(typeof(JsonStringEnumConverter<RequirementType>))]
public enum RequirementType { Aps, Labs, Rx, Mvr, Mib, Paramed }

[JsonConverter(typeof(JsonStringEnumConverter<RequirementStatus>))]
public enum RequirementStatus { Ordered, Received, Waived }

[JsonConverter(typeof(JsonStringEnumConverter<RiskClass>))]
public enum RiskClass
{
    PreferredPlus, Preferred, StandardPlus, Standard,
    Table2, Table3, Table4, Table5, Table6, Table7, Table8,
    Decline
}

[JsonConverter(typeof(JsonStringEnumConverter<ProductLine>))]
public enum ProductLine { Life, Annuity }

[JsonConverter(typeof(JsonStringEnumConverter<ProductType>))]
public enum ProductType { Term, UniversalLife, IndexedUniversalLife, FixedIndexedAnnuity, VariableAnnuity }

[JsonConverter(typeof(JsonStringEnumConverter<PolicyStatus>))]
public enum PolicyStatus { InForce, Lapsed, Pending, Surrendered }

[JsonConverter(typeof(JsonStringEnumConverter<BillingMode>))]
public enum BillingMode { Monthly, Quarterly, SemiAnnual, Annual, SinglePremium }

[JsonConverter(typeof(JsonStringEnumConverter<BeneficiaryType>))]
public enum BeneficiaryType { Primary, Contingent }

[JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
public enum TransactionType { Premium, AddressChange, BeneficiaryChange, LoanQuote, WithdrawalQuote, Loan, Withdrawal, FundTransfer, Issue }

[JsonConverter(typeof(JsonStringEnumConverter<TransactionStatus>))]
public enum TransactionStatus { Pending, Completed, Rejected }

[JsonConverter(typeof(JsonStringEnumConverter<QuoteKind>))]
public enum QuoteKind { Loan, Withdrawal }

[JsonConverter(typeof(JsonStringEnumConverter<WorklistView>))]
public enum WorklistView { Task, Case }

[JsonConverter(typeof(JsonStringEnumConverter<AlertSeverity>))]
public enum AlertSeverity { Info, Warning, Error }

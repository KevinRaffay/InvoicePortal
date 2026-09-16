using System.ComponentModel.DataAnnotations.Schema;
using InvoicePortal.Admin.Data.Abstractions;
using InvoicePortal.Admin.Services;
using StatusEnum = InvoicePortal.Admin.Data.Enums.InvoiceStatus;
using AccountTypeEnum = InvoicePortal.Admin.Data.Enums.AccountType;
using RevisionReasonEnum = InvoicePortal.Admin.Data.Enums.RevisionReason;

namespace InvoicePortal.Admin.Data.Entities;

public partial class Invoice : ISoftDeletable, IAuditable
{
    [NotMapped]
    public bool IsSoftDeleted
    {
        get => IsDeleted;
        set => IsDeleted = value;
    }

    /// <summary>Typed view over the scaffolded <c>InvoiceStatus</c> int column.</summary>
    [NotMapped]
    public StatusEnum Status
    {
        get => (StatusEnum)InvoiceStatus;
        set => InvoiceStatus = (int)value;
    }

    [NotMapped]
    public AccountTypeEnum? AccountTypeValue
    {
        get => AccountType.HasValue ? (AccountTypeEnum)AccountType.Value : null;
        set => AccountType = value.HasValue ? (int)value.Value : null;
    }

    [NotMapped]
    public RevisionReasonEnum? RevisionReasonValue
    {
        get => RevisionReason.HasValue ? (RevisionReasonEnum)RevisionReason.Value : null;
        set => RevisionReason = value.HasValue ? (int)value.Value : null;
    }

    [NotMapped]
    public string StatusName => Enum.IsDefined(typeof(StatusEnum), InvoiceStatus) ? Status.GetDisplayName() : InvoiceStatus.ToString();

    [NotMapped]
    public string AccountTypeName => EnumDisplay.DisplayNameOrEmpty<AccountTypeEnum>(AccountType);

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(Number) ? $"#{Id}" : $"#{Id} ({Number})";
}

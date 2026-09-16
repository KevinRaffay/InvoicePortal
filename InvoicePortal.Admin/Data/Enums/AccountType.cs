using System.ComponentModel.DataAnnotations;

namespace InvoicePortal.Admin.Data.Enums;

// Copied from Mec.InvoicePortal.Domain\Entities\Invoices\Enums\AccountType.cs
public enum AccountType
{
    [Display(Name = "On Premise")] OnPremise = 0,
    [Display(Name = "Off Premise")] OffPremise = 1,
}

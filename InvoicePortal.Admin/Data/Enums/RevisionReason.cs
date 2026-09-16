using System.ComponentModel.DataAnnotations;

namespace InvoicePortal.Admin.Data.Enums;

// Copied from Mec.InvoicePortal.Domain\Entities\Invoices\Enums\RevisionReason.cs.
// Members are 0-based (the DB stores 0..9); the backend's [Id] attribute is not used here.
public enum RevisionReason
{
    [Display(Name = "Updated Invoice Number")] UpdatedInvoiceNumber = 0,
    [Display(Name = "New Invoice Amount")] NewInvoiceAmount = 1,
    [Display(Name = "Incorrect Distributor")] IncorrectDistributor = 2,
    [Display(Name = "Incorrect Sales Center")] IncorrectSalesCenter = 3,
    [Display(Name = "Incorrect Payer")] IncorrectPayer = 4,
    [Display(Name = "Incorrect Brand Allocation")] IncorrectBrandAllocation = 5,
    [Display(Name = "Splitting Expenses Between Promotional Periods")] SplittingExpensesBetweenPromotionalPeriods = 6,
    [Display(Name = "Update Brand Segment")] UpdateBrandSegment = 7,
    [Display(Name = "Update Program ID")] UpdateProgramId = 8,
    [Display(Name = "Update PO #")] UpdatePONumber = 9,
}

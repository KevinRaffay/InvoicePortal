using System.ComponentModel.DataAnnotations;

namespace InvoicePortal.Admin.Data.Enums;

// Copied from Mec.InvoicePortal.Domain\Entities\Invoices\Enums\InvoiceStatus.cs (declaration order = DB value).
public enum InvoiceStatus
{
    [Display(Name = "In Process")] InProcess = 0,
    [Display(Name = "Info Required")] InfoRequired = 1,
    [Display(Name = "Paid")] Paid = 2,
    [Display(Name = "Cancellation Requested")] CancellationRequested = 3,
    [Display(Name = "Cancelled")] Cancelled = 4,
    [Display(Name = "Draft")] Draft = 5,
    [Display(Name = "Claim Logged")] ClaimLogged = 6,
    [Display(Name = "Workflow Initiated")] WorkflowInitiated = 7,
    [Display(Name = "Awaiting Claims Rep Detail")] AwaitingClaimsRepDetail = 8,
    [Display(Name = "Needs Additional Info (Rep)")] NeedsAdditionalInfoRep = 9,
    [Display(Name = "Awaiting Manager Approver")] AwaitingManagerApprover = 10,
    [Display(Name = "Awaiting Director Approval")] AwaitingDirectorApproval = 11,
    [Display(Name = "Awaiting BU VP Approval")] AwaitingBUVPApproval = 12,
    [Display(Name = "Awaiting SVP Approval")] AwaitingSVPApproval = 13,
    [Display(Name = "Awaiting President Approval")] AwaitingPresidentApproval = 14,
    [Display(Name = "Awaiting Claims Lead Approval")] AwaitingClaimsLeadApproval = 15,
    [Display(Name = "Needs Additional Info (Lead)")] NeedsAdditionalInfoLead = 16,
    [Display(Name = "Completed")] Completed = 17,
    [Display(Name = "Not Extracted")] NotExtracted = 18,
    [Display(Name = "Awaiting SD Process")] AwaitingSDProcess = 19,
    [Display(Name = "Revised")] Revised = 20,
}

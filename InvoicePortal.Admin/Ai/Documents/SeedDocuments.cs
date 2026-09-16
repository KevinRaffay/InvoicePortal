namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>
/// Six fictional customer documents standing in for the "customer documents" folder the reference app indexed.
/// Placeholders {Bottler1}..{Bottler4} are replaced with real bottler names from the local database when available.
/// </summary>
public static class SeedDocuments
{
    public const int PlaceholderCount = 4;

    public static readonly IReadOnlyList<string> FallbackNames =
        ["Northwind Beverages", "Contoso Bottling", "Fabrikam Distribution", "Adventure Works Cycles"];

    public static IReadOnlyList<SeedDocument> Build(IReadOnlyList<string> bottlerNames)
    {
        var names = Enumerable.Range(0, PlaceholderCount)
            .Select(i => i < bottlerNames.Count && !string.IsNullOrWhiteSpace(bottlerNames[i]) ? bottlerNames[i].Trim() : FallbackNames[i])
            .ToArray();

        string Fill(string s)
        {
            for (var i = 0; i < names.Length; i++)
            {
                s = s.Replace($"{{Bottler{i + 1}}}", names[i]);
            }
            return s;
        }

        return Templates
            .Select(t => new SeedDocument(t.Id, Fill(t.FileName), t.CustomerIndex is int c ? names[c] : null, Fill(t.Content)))
            .ToList();
    }

    private sealed record Template(string Id, string FileName, int? CustomerIndex, string Content);

    private static readonly Template[] Templates =
    [
        new("coop-agreement", "{Bottler1} Co-op Marketing Agreement 2025.docx", 0,
            """
            Co-op Marketing Agreement between Monster Energy and {Bottler1}, effective January 1, 2025 through December 31, 2025.
            {Bottler1} is eligible for co-op funding on approved promotional programs at a standard co-op percentage of 50 percent,
            with a maximum co-op amount per program defined in the program record. Promotional invoices must reference an approved
            program id, the promo period from and to dates, and the payer and sales center that executed the activity.
            Invoices are submitted through the Invoice Portal as type Promotional. Samples invoices are submitted separately as type
            Samples and require a samples purpose. Each invoice must carry a purchase order number unless the bottler has confirmed
            that no PO exists, in which case the "do not have PO number" flag is set and a reference document number is provided.
            Approval flows from the claims representative to the manager approver and, for amounts above the director threshold,
            to director, business unit vice president, senior vice president and president approval. Invoices are exported to SAP
            once completed and the SAP invoice number is recorded on the invoice. Revisions create a new invoice revision with a
            revision reason and keep the original under the same history correlation id.
            """),
        new("supplies-inventory", "{Bottler1} Supplies and POS Inventory.xlsx", 0,
            """
            Supplies and point-of-sale inventory for {Bottler1}, updated Q3 2025.
            Supplies associated with {Bottler1}: 120 branded glass-door coolers (model GDC-2 and GDC-3), 85 counter-top racks,
            40 end-cap display racks, 600 point-of-sale kits with shelf strips and wobblers, 200 sampling cases with 24 cans each,
            35 branded tents and 18 inflatable arches for events, and 12 mobile sampling carts.
            Cooler placements are tracked by sales center. Replacement parts for coolers (door gaskets, LED strips, thermostats)
            are ordered through the equipment program and invoiced as Promotional with the equipment program id.
            Sampling cases are consumed through the sampling program and invoiced as Samples with the samples purpose "Consumer sampling".
            Total supplies value on hand for {Bottler1}: 412,500 USD. Reorder threshold for POS kits: 150 units.
            """),
        new("qbr-q2", "{Bottler2} Quarterly Business Review Q2 2025.docx", 1,
            """
            Quarterly Business Review, {Bottler2}, second quarter 2025.
            {Bottler2} submitted 1,240 promotional invoices in Q2 with a total invoiced amount of 3.9 million in local currency,
            of which 92 percent were completed and exported to SAP within 30 days. Eleven invoices were cancelled and four were
            revised for incorrect promo period dates. The largest programs by settled amount were the summer festival sponsorship,
            the convenience channel price promotion and the cooler placement program.
            Open actions for {Bottler2}: reduce the number of invoices returned for additional information (currently 6 percent),
            add missing purchase order numbers before submission, and align payer master data for two recently merged payers.
            Next review is scheduled for October 2025.
            """),
        new("sampling-guidelines", "{Bottler3} Sampling Program Guidelines.docx", 2,
            """
            Sampling Program Guidelines for {Bottler3}.
            Samples invoices cover product given away at consumer events, retailer trainings and new account openings. {Bottler3}
            must record the samples purpose on every Samples invoice, list the sales center that distributed the product, and attach
            the event sign-off sheet. Sampling cases are limited to 500 per month per sales center unless approved by the commercial
            manager. Samples invoices are not eligible for co-op funding and are not subject to the co-op percentage.
            Invoices missing the samples purpose are placed in status Info Required until the bottler responds.
            """),
        new("payer-directory", "{Bottler4} Payer and Sales Center Directory.xlsx", 3,
            """
            Payer and Sales Center Directory for {Bottler4}, exported from the Invoice Portal lookups.
            {Bottler4} has three active payers, each with its own SAP vendor id, statement contact and remit-to address, and a total
            of fourteen sales centers. Sales centers flagged as non-VIP ship-to locations cannot be selected on promotional invoices.
            Payers marked "do not use" remain in the directory for historical invoices but are hidden from new invoice dropdowns.
            Address changes are sourced from SAP and must not be edited in the portal.
            """),
        new("submission-guide", "Invoice Portal Submission Guide.docx", null,
            """
            Invoice Portal Submission Guide for bottlers.
            Invoice statuses: Draft (saved, not submitted), In Process (submitted and awaiting claims review), Info Required
            (returned to the bottler for missing details), Claim Logged, Workflow Initiated, Awaiting Claims Rep Detail,
            Awaiting Manager Approver, Awaiting Director Approval, Awaiting BU VP Approval, Awaiting SVP Approval,
            Awaiting President Approval, Awaiting Claims Lead Approval, Completed (approved and exported to SAP), Paid,
            Cancellation Requested, Cancelled and Revised.
            Required fields: invoice number, invoice type (Promotional or Samples), invoice date, amount and currency, bottler,
            payer, sales center, program, promo period, and either a purchase order number or the no-PO confirmation.
            Invoices can be revised only while in Info Required or before Completed; a revision keeps the history correlation id
            and increments the revision number. Cancellation requests require a cancellation category.
            """),
    ];
}

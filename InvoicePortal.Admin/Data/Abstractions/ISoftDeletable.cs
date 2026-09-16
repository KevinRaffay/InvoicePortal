namespace InvoicePortal.Admin.Data.Abstractions;

/// <summary>
/// Entities whose Delete action flips a flag instead of removing the row.
/// Implemented in partial classes so scaffolded entity files stay untouched.
/// </summary>
public interface ISoftDeletable
{
    bool IsSoftDeleted { get; set; }
}

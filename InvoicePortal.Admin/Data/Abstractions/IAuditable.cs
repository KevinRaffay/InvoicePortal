namespace InvoicePortal.Admin.Data.Abstractions;

/// <summary>
/// Entities with CreatedAt / UpdatedAt columns. The database defaults both on insert
/// (sysutcdatetime()); the service stamps UpdatedAt on update.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

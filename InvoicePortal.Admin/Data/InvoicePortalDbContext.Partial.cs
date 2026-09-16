using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data;

/// <summary>
/// Hand-written half of the scaffolded context. The scaffold calls OnModelCreatingPartial at the
/// end of OnModelCreating; anything model-level that must survive a re-scaffold goes here.
/// </summary>
public partial class InvoicePortalDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Intentionally empty for the POC. Enum wrappers and marker interfaces live in
        // Data/Entities/Partials/*.Partial.cs as [NotMapped] members, so no model changes are needed.
    }
}

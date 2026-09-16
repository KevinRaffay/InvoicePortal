using System.ComponentModel.DataAnnotations.Schema;
using InvoicePortal.Admin.Data.Abstractions;

namespace InvoicePortal.Admin.Data.Entities;

public partial class Program : ISoftDeletable, IAuditable
{
    /// <summary>Programs.IsDeleted is a nullable bit; null means "not deleted".</summary>
    [NotMapped]
    public bool IsSoftDeleted
    {
        get => IsDeleted == true;
        set => IsDeleted = value;
    }

    [NotMapped]
    public string DisplayName => $"{Id} - {Name}";
}

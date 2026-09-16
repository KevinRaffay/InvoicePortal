using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Data.Entities;

[Index("Code", Name = "IX_Countries_Code", IsUnique = true)]
[Index("CreatedAt", Name = "IX_Countries_CreatedAt")]
[Index("Name", Name = "IX_Countries_Name", IsUnique = true)]
[Index("UpdatedAt", Name = "IX_Countries_UpdatedAt")]
public partial class Country
{
    [Key]
    public int Id { get; set; }

    [StringLength(3)]
    public string Code { get; set; } = null!;

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Country")]
    public virtual ICollection<Bottler> Bottlers { get; set; } = new List<Bottler>();

    [InverseProperty("Country")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

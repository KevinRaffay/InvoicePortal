using System.Text;
using InvoicePortal.Admin.Data;
using InvoicePortal.Admin.Data.Enums;
using InvoicePortal.Admin.Services;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Ai.Query;

/// <summary>
/// Produces the schema section of the NL-to-SQL system prompt from the EF Core model (no database connection needed),
/// plus the domain notes a model cannot infer from column names.
/// </summary>
public sealed class SchemaDescriber
{
    private readonly Lazy<string> schema = new(Build);

    public string Describe() => schema.Value;

    private static string Build()
    {
        var options = new DbContextOptionsBuilder<InvoicePortalDbContext>().UseSqlServer().Options;
        using var db = new InvoicePortalDbContext(options);

        var sb = new StringBuilder();
        foreach (var entity in db.Model.GetEntityTypes().OrderBy(e => e.GetTableName()))
        {
            var table = entity.GetTableName();
            if (table is null)
            {
                continue;
            }

            var columns = entity.GetProperties()
                .Select(p => $"{p.GetColumnName()} {p.GetColumnType()}{(p.IsNullable ? " null" : "")}");
            sb.Append(table).Append('(').AppendJoin(", ", columns).AppendLine(")");
        }

        sb.AppendLine();
        sb.AppendLine("Notes:");
        sb.AppendLine("- Always filter Invoices with IsDeleted = 0. Programs.IsDeleted is nullable: use (IsDeleted IS NULL OR IsDeleted = 0).");
        sb.AppendLine("- 'customer' means Bottler. Hierarchy: Bottlers -> Payers (BottlerId) -> SalesCenters (PayerId); Programs belong to Bottlers.");
        sb.AppendLine("- Invoices join to Bottlers, Payers, SalesCenters, Programs (ProgramId is a string), Currencies, Companies, Countries.");
        sb.AppendLine("- Invoices.InvoiceType is 'Promotional' or 'Samples'.");
        sb.AppendLine("- Programs has columns named [From] and [To]; always bracket them.");
        sb.Append("- Invoices.InvoiceStatus codes: ")
          .AppendJoin(", ", EnumDisplay.Options<InvoiceStatus>().Select(o => $"{(int)o.Value}={o.Text}"))
          .AppendLine(".");
        return sb.ToString();
    }
}

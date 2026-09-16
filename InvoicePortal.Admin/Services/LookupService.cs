using InvoicePortal.Admin.Data;
using InvoicePortal.Admin.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoicePortal.Admin.Services;

/// <summary>
/// Dropdown sources for edit dialogs. Lookup tables are small (hundreds to a few thousand rows)
/// so they are loaded whole and cached per circuit for a short time.
/// </summary>
public sealed class LookupService(IDbContextFactory<InvoicePortalDbContext> factory)
{
    public sealed record Option<TKey>(TKey Value, string Text);

    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);
    private readonly Dictionary<string, (DateTime At, object Data)> _cache = [];

    public Task<IReadOnlyList<Option<int>>> BottlersAsync() => Cached("bottlers", async db =>
        (IReadOnlyList<Option<int>>)await db.Bottlers.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new Option<int>(b.Id, b.Id + " - " + b.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> PayersAsync() => Cached("payers", async db =>
        (IReadOnlyList<Option<int>>)await db.Payers.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new Option<int>(p.Id, p.Id + " - " + p.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> PayersForBottlerAsync(int? bottlerId) => Cached($"payers:{bottlerId}", async db =>
        (IReadOnlyList<Option<int>>)await db.Payers.AsNoTracking()
            .Where(p => bottlerId == null || p.BottlerId == bottlerId)
            .OrderBy(p => p.Name)
            .Select(p => new Option<int>(p.Id, p.Id + " - " + p.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> SalesCentersAsync() => Cached("salescenters", async db =>
        (IReadOnlyList<Option<int>>)await db.SalesCenters.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new Option<int>(s.Id, s.Id + " - " + s.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> SalesCentersForPayerAsync(int? payerId) => Cached($"salescenters:{payerId}", async db =>
        (IReadOnlyList<Option<int>>)await db.SalesCenters.AsNoTracking()
            .Where(s => payerId == null || s.PayerId == payerId)
            .OrderBy(s => s.Name)
            .Select(s => new Option<int>(s.Id, s.Id + " - " + s.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<string>>> ProgramsForBottlerAsync(int? bottlerId) => Cached($"programs:{bottlerId}", async db =>
        (IReadOnlyList<Option<string>>)await db.Programs.AsNoTracking()
            .Where(p => p.IsDeleted != true && (bottlerId == null || p.BottlerId == bottlerId))
            .OrderBy(p => p.Id)
            .Select(p => new Option<string>(p.Id, p.Id + " - " + p.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> CurrenciesAsync() => Cached("currencies", async db =>
        (IReadOnlyList<Option<int>>)await db.Currencies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new Option<int>(c.Id, c.Code))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> CompaniesAsync() => Cached("companies", async db =>
        (IReadOnlyList<Option<int>>)await db.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new Option<int>(c.Id, c.Code + " - " + c.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> CountriesAsync() => Cached("countries", async db =>
        (IReadOnlyList<Option<int>>)await db.Countries.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new Option<int>(c.Id, c.Code + " - " + c.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<int>>> BrandSegmentCategoriesAsync() => Cached("bsc", async db =>
        (IReadOnlyList<Option<int>>)await db.BrandSegmentCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new Option<int>(c.Id, c.Name))
            .ToListAsync());

    public Task<IReadOnlyList<Option<string>>> SalesOrganizationsAsync() => Cached("salesorgs", async db =>
        (IReadOnlyList<Option<string>>)await db.Bottlers.AsNoTracking()
            .Select(b => b.SalesOrganization)
            .Distinct()
            .OrderBy(s => s)
            .Select(s => new Option<string>(s, s))
            .ToListAsync());

    public sealed record InvoiceUser(Guid UserId, string UserName, string UserEmail);

    /// <summary>
    /// Invoices.UserId is a required FK to AspNetUsers (not managed by this app). New invoices default
    /// to the submitter of the most recent invoice so the insert satisfies the constraint.
    /// </summary>
    public Task<IReadOnlyList<InvoiceUser>> RecentInvoiceUsersAsync() => Cached("invoiceusers", async db =>
        (IReadOnlyList<InvoiceUser>)await db.Invoices.AsNoTracking()
            .Where(i => i.UserEmail != "")
            .Where(i => !db.Invoices.Any(other =>
                other.UserId == i.UserId
                && (other.CreatedAt > i.CreatedAt
                    || (other.CreatedAt == i.CreatedAt && other.Id > i.Id))))
            .OrderByDescending(i => i.CreatedAt)
            .Take(50)
            .Select(i => new InvoiceUser(i.UserId, i.UserName, i.UserEmail))
            .ToListAsync());

    public static IReadOnlyList<Option<string>> InvoiceTypes { get; } =
    [
        new("Promotional", "Promotional"),
        new("Samples", "Samples"),
    ];

    public void Invalidate() => _cache.Clear();

    private async Task<T> Cached<T>(string key, Func<InvoicePortalDbContext, Task<T>> load) where T : class
    {
        if (_cache.TryGetValue(key, out var hit) && DateTime.UtcNow - hit.At < CacheFor)
        {
            return (T)hit.Data;
        }

        await using var db = await factory.CreateDbContextAsync();
        var data = await load(db);
        _cache[key] = (DateTime.UtcNow, data);
        return data;
    }
}

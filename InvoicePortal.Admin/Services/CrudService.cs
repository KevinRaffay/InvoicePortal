using System.Linq.Expressions;
using InvoicePortal.Admin.Data;
using InvoicePortal.Admin.Data.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Radzen;

namespace InvoicePortal.Admin.Services;

/// <summary>
/// Generic list / get / insert / update / delete over a pooled DbContext factory.
/// Filtering and sorting come straight from RadzenDataGrid's LoadDataArgs and are translated
/// to SQL by Radzen's expression-based QueryableExtension (no Dynamic LINQ).
/// </summary>
public sealed class CrudService<TEntity>(IDbContextFactory<InvoicePortalDbContext> factory)
    where TEntity : class
{
    public sealed record PagedResult(IReadOnlyList<TEntity> Items, int Count);

    public async Task<PagedResult> LoadAsync(
        LoadDataArgs args,
        bool includeSoftDeleted = false,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? shape = null,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        IQueryable<TEntity> query = db.Set<TEntity>().AsNoTracking();

        if (shape is not null)
        {
            query = shape(query);
        }

        if (!includeSoftDeleted && SoftDelete.NotDeleted<TEntity>() is { } notDeleted)
        {
            query = query.Where(notDeleted);
        }

        if (args.Filters is not null && args.Filters.Any())
        {
            query = query.Where(args.Filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive);
        }

        var count = await query.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(args.OrderBy))
        {
            query = query.OrderBy(args.OrderBy);
        }
        else if (DefaultOrder is not null)
        {
            query = DefaultOrder(query);
        }

        if (args.Skip is > 0)
        {
            query = query.Skip(args.Skip.Value);
        }

        if (args.Top is > 0)
        {
            query = query.Take(args.Top.Value);
        }

        var items = await query.ToListAsync(ct);
        return new PagedResult(items, count);
    }

    /// <summary>Optional stable ordering used when the grid has no sort applied (keeps paging deterministic).</summary>
    public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? DefaultOrder { get; set; }

    public async Task<TEntity?> FindAsync(object key, Func<IQueryable<TEntity>, IQueryable<TEntity>>? shape = null, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (shape is null)
        {
            return await db.Set<TEntity>().FindAsync([key], ct);
        }

        var entityType = db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the model.");
        var pk = entityType.FindPrimaryKey()?.Properties.Single()
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no single-column primary key.");

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var body = Expression.Equal(
            Expression.Property(parameter, pk.PropertyInfo!),
            Expression.Constant(key, pk.ClrType));
        var predicate = Expression.Lambda<Func<TEntity, bool>>(body, parameter);

        return await shape(db.Set<TEntity>().AsNoTracking()).FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<bool> ExistsAsync(object key, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Set<TEntity>().FindAsync([key], ct) is not null;
    }

    public async Task<int> CountAsync(bool includeSoftDeleted = false, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        IQueryable<TEntity> query = db.Set<TEntity>();
        if (!includeSoftDeleted && SoftDelete.NotDeleted<TEntity>() is { } notDeleted)
        {
            query = query.Where(notDeleted);
        }
        return await query.CountAsync(ct);
    }

    public async Task InsertAsync(TEntity entity, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        DetachNavigations(db, entity);
        db.Set<TEntity>().Add(entity);
        await SaveAsync(db, ct);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        DetachNavigations(db, entity);

        if (entity is IAuditable auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
        }

        MarkModified(db, entity);
        await SaveAsync(db, ct);
    }

    /// <summary>
    /// Attaches a detached entity as Modified while leaving computed columns and CreatedAt alone.
    /// UpdatedAt is explicitly marked modified because its DB default makes EF treat it as store-generated.
    /// </summary>
    private static void MarkModified(InvoicePortalDbContext db, TEntity entity)
    {
        var entry = db.Set<TEntity>().Attach(entity);
        entry.State = EntityState.Modified;

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate
                || property.Metadata.GetComputedColumnSql() is not null)
            {
                property.IsModified = false;
            }
        }

        if (entity is IAuditable)
        {
            entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            entry.Property(nameof(IAuditable.UpdatedAt)).IsModified = true;
        }
    }

    /// <summary>Soft-deletes when the entity implements <see cref="ISoftDeletable"/>, otherwise removes the row.</summary>
    public async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        DetachNavigations(db, entity);

        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsSoftDeleted = true;
            if (entity is IAuditable auditable)
            {
                auditable.UpdatedAt = DateTime.UtcNow;
            }
            MarkModified(db, entity);
        }
        else
        {
            db.Set<TEntity>().Remove(entity);
        }

        await SaveAsync(db, ct);
    }

    public async Task RestoreAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity is not ISoftDeletable softDeletable)
        {
            return;
        }

        await using var db = await factory.CreateDbContextAsync(ct);
        DetachNavigations(db, entity);
        softDeletable.IsSoftDeleted = false;
        if (entity is IAuditable auditable)
        {
            auditable.UpdatedAt = DateTime.UtcNow;
        }
        MarkModified(db, entity);
        await SaveAsync(db, ct);
    }

    /// <summary>
    /// Entities loaded for grids carry navigation objects (Bottler, Payer, ...). Attaching them would
    /// try to track/insert the related rows too, so clear reference navigations before attach.
    /// </summary>
    private static void DetachNavigations(InvoicePortalDbContext db, TEntity entity)
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity));
        if (entityType is null)
        {
            return;
        }

        foreach (var navigation in entityType.GetNavigations())
        {
            navigation.PropertyInfo?.SetValue(entity, null);
        }
    }

    private static async Task SaveAsync(InvoicePortalDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            throw new CrudException(TranslateSqlError(sql), ex);
        }
    }

    private static string TranslateSqlError(SqlException ex) => ex.Number switch
    {
        2601 or 2627 => "A row with the same unique value already exists.",
        547 when ex.Message.Contains("DELETE", StringComparison.OrdinalIgnoreCase)
            => "This row is referenced by other records and cannot be deleted.",
        547 => "A referenced record does not exist (foreign key violation). Check the selected lookups.",
        515 => "A required column was left empty: " + ex.Message,
        _ => $"Database error {ex.Number}: {ex.Message}",
    };
}

public sealed class CrudException(string message, Exception inner) : Exception(message, inner);

internal static class SoftDelete
{
    /// <summary>
    /// Builds <c>e =&gt; !e.IsDeleted</c> (bool column) or <c>e =&gt; e.IsDeleted != true</c> (nullable bit column)
    /// against the mapped column, so EF can translate it to SQL. The ISoftDeletable wrapper is [NotMapped].
    /// </summary>
    public static Expression<Func<TEntity, bool>>? NotDeleted<TEntity>()
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            return null;
        }

        var column = typeof(TEntity).GetProperty("IsDeleted")
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} implements ISoftDeletable but has no IsDeleted column.");

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var member = Expression.Property(parameter, column);
        Expression body = column.PropertyType == typeof(bool)
            ? Expression.Not(member)
            : Expression.NotEqual(member, Expression.Constant(true, typeof(bool?)));

        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }
}

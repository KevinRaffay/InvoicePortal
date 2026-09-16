using InvoicePortal.Admin.Data.Abstractions;
using InvoicePortal.Admin.Services;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace InvoicePortal.Admin.Components.Shared;

/// <summary>
/// Code-behind shared by every CRUD grid page: server-side LoadData, Add/Edit via a dialog
/// component, Delete with confirmation, and the "Show deleted" toggle for soft-deletable entities.
/// Pages supply the markup (RadzenDataGrid + columns) and override the few hooks below.
/// </summary>
public abstract class CrudPageBase<TEntity, TDialog> : ComponentBase
    where TEntity : class
    where TDialog : ComponentBase
{
    [Inject] protected CrudService<TEntity> Service { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService Notifications { get; set; } = default!;

    protected RadzenDataGrid<TEntity>? Grid;

    /// <summary>Stays null until the first LoadData: RadzenDataGrid only auto-invokes LoadData on first render when Data is null.</summary>
    protected IReadOnlyList<TEntity>? Items;
    protected int Count;
    protected bool IsLoading;
    protected bool ShowDeleted;

    protected static bool SupportsSoftDelete => typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity));

    /// <summary>Human-readable singular name used in dialog titles and notifications.</summary>
    protected abstract string EntityName { get; }

    /// <summary>Primary-key value of an entity, passed to the dialog as the "Id" parameter.</summary>
    protected abstract object KeyOf(TEntity entity);

    /// <summary>Short description of a row for confirmation dialogs.</summary>
    protected virtual string Describe(TEntity entity) => $"{EntityName} {KeyOf(entity)}";

    /// <summary>Apply Includes / projections needed by the grid columns.</summary>
    protected virtual IQueryable<TEntity> Shape(IQueryable<TEntity> query) => query;

    protected virtual string DialogWidth => "760px";

    protected async Task LoadData(LoadDataArgs args)
    {
        IsLoading = true;
        try
        {
            var result = await Service.LoadAsync(args, ShowDeleted, Shape);
            Items = result.Items;
            Count = result.Count;
        }
        catch (Exception ex)
        {
            Notify(NotificationSeverity.Error, "Load failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task Reload()
    {
        if (Grid is not null)
        {
            await Grid.Reload();
        }
    }

    protected async Task ToggleShowDeleted(bool value)
    {
        ShowDeleted = value;
        await Reload();
    }

    protected async Task Add()
    {
        var saved = await DialogService.OpenAsync<TDialog>($"New {EntityName}",
            new Dictionary<string, object?> { },
            new DialogOptions { Width = DialogWidth, Resizable = true, Draggable = true, CloseDialogOnOverlayClick = false });

        if (saved is true)
        {
            Notify(NotificationSeverity.Success, "Created", $"{EntityName} created.");
            await Reload();
        }
    }

    protected async Task Edit(TEntity entity)
    {
        var saved = await DialogService.OpenAsync<TDialog>($"Edit {Describe(entity)}",
            new Dictionary<string, object?> { ["Id"] = KeyOf(entity) },
            new DialogOptions { Width = DialogWidth, Resizable = true, Draggable = true, CloseDialogOnOverlayClick = false });

        if (saved is true)
        {
            Notify(NotificationSeverity.Success, "Saved", $"{Describe(entity)} updated.");
            await Reload();
        }
    }

    protected async Task Delete(TEntity entity)
    {
        var soft = entity is ISoftDeletable;
        var confirmed = await DialogService.OpenAsync<ConfirmDelete>(soft ? "Mark as deleted" : "Delete permanently",
            new Dictionary<string, object?>
            {
                ["Message"] = soft
                    ? $"Mark {Describe(entity)} as deleted? It will be hidden from the grid but kept in the database."
                    : $"Permanently delete {Describe(entity)}?",
                ["Detail"] = soft ? string.Empty : "Rows in dependent tables may be removed by database cascade rules.",
                ["ConfirmText"] = soft ? "Mark deleted" : "Delete",
            },
            new DialogOptions { Width = "480px" });

        if (confirmed is not true)
        {
            return;
        }

        try
        {
            await Service.DeleteAsync(entity);
            Notify(NotificationSeverity.Success, soft ? "Marked deleted" : "Deleted", $"{Describe(entity)} {(soft ? "marked as deleted" : "deleted")}.");
            await Reload();
        }
        catch (CrudException ex)
        {
            Notify(NotificationSeverity.Error, "Delete failed", ex.Message);
        }
    }

    protected async Task Restore(TEntity entity)
    {
        try
        {
            await Service.RestoreAsync(entity);
            Notify(NotificationSeverity.Success, "Restored", $"{Describe(entity)} restored.");
            await Reload();
        }
        catch (CrudException ex)
        {
            Notify(NotificationSeverity.Error, "Restore failed", ex.Message);
        }
    }

    protected void Notify(NotificationSeverity severity, string summary, string detail)
        => Notifications.Notify(new NotificationMessage { Severity = severity, Summary = summary, Detail = detail, Duration = 5000 });
}

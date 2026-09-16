using InvoicePortal.Admin.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace InvoicePortal.Admin.Components.Shared;

/// <summary>
/// Code-behind shared by every edit dialog: loads the entity by Id (or creates a new one),
/// saves through the CrudService, and closes the dialog with <c>true</c> on success.
/// </summary>
public abstract class EditDialogBase<TEntity> : ComponentBase
    where TEntity : class, new()
{
    [Inject] protected CrudService<TEntity> Service { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService Notifications { get; set; } = default!;
    [Inject] protected LookupService Lookups { get; set; } = default!;

    /// <summary>Primary key of the row to edit; null means "create".</summary>
    [Parameter] public object? Id { get; set; }

    protected TEntity Model { get; set; } = new();
    protected bool IsNew => Id is null;
    protected bool IsBusy;
    protected string? Error;

    protected override async Task OnInitializedAsync()
    {
        if (Id is not null)
        {
            var existing = await Service.FindAsync(Id);
            if (existing is null)
            {
                Error = "The record no longer exists.";
                return;
            }
            Model = existing;
        }
        else
        {
            Model = CreateNew();
        }

        await OnModelLoadedAsync();
    }

    /// <summary>Override to seed defaults on a new record.</summary>
    protected virtual TEntity CreateNew() => new();

    /// <summary>Override to load dropdown sources after the model is available.</summary>
    protected virtual Task OnModelLoadedAsync() => Task.CompletedTask;

    /// <summary>Override for validation that spans fields or needs the DB (e.g. manual key uniqueness).</summary>
    protected virtual Task<string?> ValidateAsync() => Task.FromResult<string?>(null);

    protected async Task Submit()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Error = null;
        try
        {
            Error = await ValidateAsync();
            if (Error is not null)
            {
                return;
            }

            if (IsNew)
            {
                await Service.InsertAsync(Model);
            }
            else
            {
                await Service.UpdateAsync(Model);
            }

            DialogService.Close(true);
        }
        catch (CrudException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            Error = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void Cancel() => DialogService.Close(false);
}

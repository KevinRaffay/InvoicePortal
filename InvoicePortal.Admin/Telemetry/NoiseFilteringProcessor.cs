using System.Diagnostics;
using OpenTelemetry;

namespace InvoicePortal.Admin.Telemetry;

/// <summary>
/// Keeps the Blazor Server plumbing spans that .NET 10 emits for every SignalR hub call (render acknowledgements,
/// JS interop completions, location-change notifications) out of the export pipeline. Without this, a single page
/// interaction produces dozens of one-span traces that bury the useful ones. The user action itself is still traced:
/// Blazor's <c>Event onclick -> ...</c> span is the root of the query work. Circuit start and root-component updates
/// are kept because they carry real rendering and database work.
/// </summary>
/// <remarks>
/// This is a processor rather than a sampler because SignalR creates the hub activity under a constant name and only
/// afterwards sets its display name and <c>rpc.*</c> tags, so a sampler never sees the method name. Clearing the
/// <see cref="ActivityTraceFlags.Recorded"/> flag in <see cref="OnEnd"/> makes every later export processor skip the span.
/// Register it before the exporters.
/// </remarks>
public sealed class NoiseFilteringProcessor : BaseProcessor<Activity>
{
    private const string ComponentHubPrefix = "Microsoft.AspNetCore.Components.Server.ComponentHub/";
    private const string ComponentHubService = "Microsoft.AspNetCore.Components.Server.ComponentHub";

    private static readonly HashSet<string> DroppedHubMethods = new(StringComparer.Ordinal)
    {
        "OnRenderCompleted",
        "EndInvokeJSFromDotNet",
        "BeginInvokeDotNetFromJS",
        "ReceiveJSDataChunk",
        "ReceiveByteArray",
        "OnLocationChanged",
        "OnLocationChanging",
    };

    public override void OnEnd(Activity data)
    {
        if (IsNoiseSpan(data.DisplayName, data.GetTagItem("rpc.service") as string, data.GetTagItem("rpc.method") as string))
        {
            data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }

    /// <summary>True for a SignalR hub-method span that is Blazor plumbing rather than application work.</summary>
    public static bool IsNoiseSpan(string? displayName, string? rpcService = null, string? rpcMethod = null)
    {
        if (rpcService == ComponentHubService && rpcMethod is not null)
        {
            return DroppedHubMethods.Contains(rpcMethod);
        }

        return displayName is not null
            && displayName.StartsWith(ComponentHubPrefix, StringComparison.Ordinal)
            && DroppedHubMethods.Contains(displayName[ComponentHubPrefix.Length..]);
    }
}

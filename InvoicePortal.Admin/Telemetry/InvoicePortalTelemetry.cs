using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace InvoicePortal.Admin.Telemetry;

/// <summary>
/// The application's own instrumentation names. Spans and metrics from these are only exported when the
/// tracer/meter provider subscribes to them (see <see cref="TelemetryExtensions"/>); otherwise
/// <c>StartActivity</c> returns null and the counters are no-ops, so the call sites cost nothing.
/// </summary>
public static class InvoicePortalTelemetry
{
    /// <summary>Custom spans: AI query generation, guarded SQL execution, grounded answers.</summary>
    public const string SourceName = "InvoicePortal.Admin";

    /// <summary>
    /// Source and meter name used by <c>Microsoft.Extensions.AI</c>'s <c>UseOpenTelemetry()</c> decorator on the
    /// chat pipeline (one <c>chat</c> span per model call with token usage, following the GenAI semantic conventions).
    /// </summary>
    public const string AiSourceName = "InvoicePortal.Admin.Ai";

    public static readonly ActivitySource Source = new(SourceName);

    public static readonly Meter Meter = new(SourceName);

    /// <summary>One increment per AI feature request. Tags: <c>ai.feature</c> (query, documents), <c>ai.outcome</c>.</summary>
    public static readonly Counter<long> AiRequests = Meter.CreateCounter<long>(
        "invoiceportal.ai.requests",
        unit: "{request}",
        description: "AI feature requests by feature and outcome.");

    /// <summary>Rows returned by generated SQL, before the row cap truncates them.</summary>
    public static readonly Histogram<int> AiQueryRows = Meter.CreateHistogram<int>(
        "invoiceportal.ai.query.rows",
        unit: "{row}",
        description: "Rows returned by AI-generated SQL.");

    public static class Feature
    {
        public const string Query = "query";
        public const string Documents = "documents";
    }

    public static class Outcome
    {
        public const string Ok = "ok";
        /// <summary>The model answered with an error object or the guard rejected the SQL: no exception, no result.</summary>
        public const string Rejected = "rejected";
        public const string Error = "error";
    }

    public static void RecordAiRequest(string feature, string outcome) =>
        AiRequests.Add(1, new KeyValuePair<string, object?>("ai.feature", feature), new KeyValuePair<string, object?>("ai.outcome", outcome));

    /// <summary>Marks the activity failed and records the exception as an event, following the OTel exception conventions.</summary>
    public static void RecordException(this Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }
}

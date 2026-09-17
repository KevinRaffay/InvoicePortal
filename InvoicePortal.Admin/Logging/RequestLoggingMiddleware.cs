using System.Diagnostics;
using InvoicePortal.Admin.Telemetry;

namespace InvoicePortal.Admin.Logging;

/// <summary>
/// One log record per HTTP request: method, path, status code and elapsed milliseconds.
/// <para>
/// It logs through <see cref="ILogger{TCategoryName}"/> rather than Serilog's own
/// <c>UseSerilogRequestLogging</c> on purpose. Serilog is registered as a logging
/// <em>provider</em> (see <see cref="LoggingExtensions"/>), and OpenTelemetry adds a second
/// provider for OTLP. Only records written through Microsoft.Extensions.Logging reach both, so
/// this is what puts request logs in the Aspire dashboard as well as the console and files.
/// <c>UseSerilogRequestLogging</c> writes straight to Serilog and would skip the OTLP exporter.
/// </para>
/// <para>
/// The category is this class, not <c>Microsoft.AspNetCore</c>, so these records are not affected
/// by the <c>Serilog:MinimumLevel:Override</c> that holds the framework's own request logging at
/// Warning. That override stays in place: it is what stops the duplicate, three-lines-per-request
/// framework output.
/// </para>
/// <para>
/// Noise is filtered with the same <see cref="TelemetryExtensions.IsNoiseRequest"/> predicate the
/// tracing pipeline uses, so logs and traces agree on what counts as a real request. In a Blazor
/// Server app that means page loads and endpoint calls are logged, while the health probe, static
/// assets and the <c>/_blazor</c> circuit traffic are not - interactions inside a circuit are not
/// HTTP requests and show up as spans, not request logs.
/// </para>
/// </summary>
internal sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (TelemetryExtensions.IsNoiseRequest(context.Request.Path))
        {
            await next(context);
            return;
        }

        var start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Logged here so a failed request still produces a request record, then rethrown for
            // the exception handler to turn into a response.
            logger.LogError(
                ex,
                "HTTP {RequestMethod:l} {RequestPath:l} threw after {ElapsedMilliseconds:0.0000} ms",
                context.Request.Method,
                context.Request.Path.Value,
                Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            throw;
        }

        var status = context.Response.StatusCode;
        var level = status >= 500 ? LogLevel.Error
            : status >= 400 ? LogLevel.Warning
            : LogLevel.Information;

        // ":l" renders the strings unquoted in Serilog's output; Microsoft.Extensions.Logging
        // ignores a format specifier on a plain string, so the OTLP record is unaffected.
        logger.Log(
            level,
            "HTTP {RequestMethod:l} {RequestPath:l} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
            context.Request.Method,
            context.Request.Path.Value,
            status,
            Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }
}

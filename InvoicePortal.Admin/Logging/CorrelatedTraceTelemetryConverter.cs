using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace InvoicePortal.Admin.Logging;

/// <summary>Use the Activity captured in the event, not the batching thread's ambient Activity.</summary>
public sealed class CorrelatedTraceTelemetryConverter(string serviceName, string instanceName) : TraceTelemetryConverter
{
    public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
    {
        foreach (var telemetry in base.Convert(logEvent, formatProvider))
        {
            telemetry.Context.Cloud.RoleName = serviceName;
            telemetry.Context.Cloud.RoleInstance = instanceName;
            if (logEvent.TraceId is { } traceId)
                telemetry.Context.Operation.Id = traceId.ToHexString();
            if (logEvent.SpanId is { } spanId)
                telemetry.Context.Operation.ParentId = spanId.ToHexString();
            yield return telemetry;
        }
    }
}
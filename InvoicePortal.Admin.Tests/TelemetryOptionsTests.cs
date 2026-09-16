using System.ComponentModel.DataAnnotations;
using InvoicePortal.Admin.Telemetry;
using Microsoft.AspNetCore.Http;

namespace InvoicePortal.Admin.Tests;

public class TelemetryOptionsTests
{
    private static bool IsValid(TelemetryOptions options)
        => Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true);

    [Fact]
    public void Defaults_export_nothing_and_record_no_sensitive_content()
    {
        var o = new TelemetryOptions();

        Assert.True(o.Enabled);
        Assert.Null(o.OtlpEndpoint);
        Assert.Null(o.AzureMonitorConnectionString);
        Assert.False(o.RecordSqlText);
        Assert.False(o.RecordAiContent);
        Assert.Equal(1.0, o.TraceSamplingRatio);
        Assert.True(IsValid(o));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("grpc", true)]
    [InlineData("http/protobuf", true)]
    [InlineData("http", false)]
    [InlineData("HTTP/JSON", false)]
    public void Only_known_otlp_protocols_are_valid(string protocol, bool expected)
    {
        Assert.Equal(expected, IsValid(new TelemetryOptions { OtlpProtocol = protocol }));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Sampling_ratio_outside_zero_to_one_is_rejected(double ratio)
    {
        Assert.False(IsValid(new TelemetryOptions { TraceSamplingRatio = ratio }));
    }

    [Theory]
    [InlineData("/healthz", true)]
    [InlineData("/_framework/blazor.web.js", true)]
    [InlineData("/_blazor", true)]
    [InlineData("/_content/Radzen.Blazor/css/material-base.css", true)]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css", true)]
    [InlineData("/app.css", true)]
    [InlineData("/favicon.ico", true)]
    [InlineData("/", false)]
    [InlineData("/invoices", false)]
    [InlineData("/ai/query", false)]
    public void Health_probe_and_static_assets_are_not_traced(string path, bool noise)
    {
        Assert.Equal(noise, TelemetryExtensions.IsNoiseRequest(new PathString(path)));
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub/OnRenderCompleted", true)]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub/EndInvokeJSFromDotNet", true)]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub/BeginInvokeDotNetFromJS", true)]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub/OnLocationChanged", true)]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub/StartCircuit", false)]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub/UpdateRootComponents", false)]
    [InlineData("Event onclick -> Radzen.Blazor.RadzenButton.OnClick", false)]
    [InlineData("ai.query.generate", false)]
    [InlineData("GET /ai/query", false)]
    [InlineData(null, false)]
    public void Blazor_hub_plumbing_spans_are_dropped_but_events_and_work_are_kept(string? spanName, bool noise)
    {
        Assert.Equal(noise, NoiseFilteringProcessor.IsNoiseSpan(spanName));
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub", "OnRenderCompleted", true)]
    [InlineData("Microsoft.AspNetCore.Components.Server.ComponentHub", "StartCircuit", false)]
    [InlineData("Some.Other.Hub", "OnRenderCompleted", false)]
    public void Hub_plumbing_is_recognised_from_rpc_tags_when_the_display_name_is_generic(string service, string method, bool noise)
    {
        Assert.Equal(noise, NoiseFilteringProcessor.IsNoiseSpan("Microsoft.AspNetCore.SignalR.Server.InvocationIn", service, method));
    }
}

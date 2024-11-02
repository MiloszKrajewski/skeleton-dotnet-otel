namespace K4os.WebApp.Bootstrap;

public class OpenTelemetryConfig
{
    public Uri TelemetryEndpoint { get; set; } = new("http://localhost:4317");
    public string[] Traces { get; set; } = [];
    public string[] Meters { get; set; } = [];
}

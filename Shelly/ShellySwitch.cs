using System.Net.Http.Json;
using System.Text.Json;

namespace Shelly;

public sealed class ShellySwitch
{
    private readonly HttpClient httpClient;

    public ShellySwitch(string name, string ipAddress, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Switch name is required.", nameof(name));
        else if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentException("Switch IP address is required.", nameof(ipAddress));
        else
        {
            Name = name.Trim();
            IpAddress = ipAddress.Trim();
            this.httpClient = httpClient ?? new HttpClient();
        }
    }

    public string Name { get; set; }

    public string IpAddress { get; set; }

    public Uri BaseUri => new($"https://{IpAddress}");

    public Task<JsonDocument> ReadStatusAsync(CancellationToken cancellationToken = default)
    {
        return SendRpcCallAsync("Switch.GetStatus?id=0", cancellationToken);
    }

    public Task TurnOnAsync(CancellationToken cancellationToken = default)
    {
        return SetSwitchStateAsync(true, cancellationToken);
    }

    public Task TurnOffAsync(CancellationToken cancellationToken = default)
    {
        return SetSwitchStateAsync(false, cancellationToken);
    }

    private async Task SetSwitchStateAsync(bool isOn, CancellationToken cancellationToken)
    {
        using JsonDocument response = await SendRpcCallAsync(
            $"Switch.Set?id=0&on={isOn.ToString().ToLowerInvariant()}",
            cancellationToken);
    }

    private async Task<JsonDocument> SendRpcCallAsync(string rpcCommand, CancellationToken cancellationToken)
    {
        Uri requestUri = new(BaseUri, $"/rpc/{rpcCommand}");
        using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        JsonDocument? body = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        return body ?? JsonDocument.Parse("{}");
    }
}

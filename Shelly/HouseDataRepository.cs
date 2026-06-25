using System.Text.Json;

namespace Shelly;

public sealed class HouseDataRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string dataFilePath = Path.Combine(
        FileSystem.AppDataDirectory,
        "house-data.json");

    public async Task<HouseData> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(dataFilePath))
        {
            return new HouseData();
        }

        await using FileStream stream = File.OpenRead(dataFilePath);
        HouseData? data = await JsonSerializer.DeserializeAsync<HouseData>(
            stream,
            JsonOptions,
            cancellationToken);

        return data ?? new HouseData();
    }

    public async Task SaveAsync(HouseData data, CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(dataFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = File.Create(dataFilePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
    }
}

public sealed class HouseData
{
    public Guid? LastSceneId { get; set; }

    public float LastYaw { get; set; }

    public float LastPitch { get; set; }

    public List<PanoramaScene> Scenes { get; set; } = [];

    public List<DeviceHotspot> DeviceHotspots { get; set; } = [];

    public List<LocationHotspot> LocationHotspots { get; set; } = [];
}

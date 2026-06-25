namespace Shelly;

public sealed class LocationHotspot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SourceSceneId { get; set; }

    public Guid TargetSceneId { get; set; }

    public float Yaw { get; set; }

    public float Pitch { get; set; }
}

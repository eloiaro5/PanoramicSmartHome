namespace Shelly;

/// <summary>A scene button that controls devices belonging to a named group.</summary>
public sealed class GroupHotspot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SceneId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public float Yaw { get; set; }
    public float Pitch { get; set; }
}

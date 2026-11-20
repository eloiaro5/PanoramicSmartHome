using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shelly;

public sealed class DeviceHotspot : INotifyPropertyChanged
{
    private DeviceState currentState = DeviceState.Waiting;
    private string ipAddress = string.Empty;
    private string name = string.Empty;
    private float pitch;
    private float yaw;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SceneId { get; set; }

    /// <summary>Names of the groups this device belongs to.</summary>
    public List<string> GroupNames { get; set; } = [];

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string IpAddress
    {
        get => ipAddress;
        set => SetProperty(ref ipAddress, value);
    }

    public float Yaw
    {
        get => yaw;
        set => SetProperty(ref yaw, value);
    }

    public float Pitch
    {
        get => pitch;
        set => SetProperty(ref pitch, value);
    }

    public DeviceState CurrentState
    {
        get => currentState;
        set => SetProperty(ref currentState, value);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum DeviceState
{
    Waiting,
    On,
    Off,
    Failed
}

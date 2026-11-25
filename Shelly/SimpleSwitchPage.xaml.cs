namespace Shelly;

/// <summary>A full-screen switch surface with access to the scene map.</summary>
public partial class SimpleSwitchPage : ContentPage
{
    private readonly Func<Task>? toggleAsync;

    public SimpleSwitchPage()
    {
        InitializeComponent();
    }

    /// <summary>Creates a switch surface that invokes the selected switch action.</summary>
    public SimpleSwitchPage(Func<Task> toggleAsync) : this()
    {
        ArgumentNullException.ThrowIfNull(toggleAsync);
        this.toggleAsync = toggleAsync;
    }

    /// <summary>Raised when the user presses the switch surface.</summary>
    public event EventHandler? SwitchRequested;

    private async void OnSwitchClicked(object? sender, EventArgs e)
    {
        SwitchButton.IsEnabled = false;
        try
        {
            SwitchRequested?.Invoke(this, EventArgs.Empty);
            if (toggleAsync is not null) await toggleAsync();
        }
        finally
        {
            SwitchButton.IsEnabled = true;
        }
    }

    private async void OnMapClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

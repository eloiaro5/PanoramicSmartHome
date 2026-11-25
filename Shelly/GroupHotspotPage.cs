using System.Globalization;

namespace Shelly;

/// <summary>Creates or edits a group button in a panorama scene.</summary>
public sealed class GroupHotspotPage : ContentPage
{
    private readonly Guid sceneId;
    private readonly GroupHotspot? group;
    private readonly Picker groupPicker = new() { Title = "Choose a group" };
    private readonly Entry yawEntry = new() { Text = "0", Keyboard = Keyboard.Numeric };
    private readonly Entry pitchEntry = new() { Text = "0", Keyboard = Keyboard.Numeric };

    public event EventHandler<GroupHotspot>? GroupCreated;
    public event EventHandler<GroupHotspot>? GroupUpdated;
    public event EventHandler<GroupHotspot>? GroupDeleted;

    public GroupHotspotPage(IReadOnlyList<DeviceHotspot> devices, Guid sceneId, GroupHotspot? group = null)
    {
        this.sceneId = sceneId;
        this.group = group;
        Title = group is null ? "Add group button" : "Edit group button";
        BackgroundColor = Color.FromArgb("#111814");

        List<string> names = [.. devices.SelectMany(device => device.GroupNames)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
        if (group is not null && !names.Contains(group.GroupName, StringComparer.OrdinalIgnoreCase)) names.Add(group.GroupName);
        groupPicker.ItemsSource = names;
        groupPicker.TextColor = Color.FromArgb("#111814");
        yawEntry.TextColor = pitchEntry.TextColor = Color.FromArgb("#111814");
        if (group is not null)
        {
            groupPicker.SelectedItem = names.First(name => string.Equals(name, group.GroupName, StringComparison.OrdinalIgnoreCase));
            yawEntry.Text = group.Yaw.ToString(CultureInfo.InvariantCulture);
            pitchEntry.Text = group.Pitch.ToString(CultureInfo.InvariantCulture);
        }

        var cancel = new Button { Text = "Cancel", BackgroundColor = Color.FromArgb("#A65A5A") };
        var save = new Button { Text = "Save", BackgroundColor = Color.FromArgb("#5F8A6A"), TextColor = Colors.White };
        cancel.Clicked += async (_, _) => await Navigation.PopModalAsync();
        save.Clicked += OnSaveClicked;
        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 12 };
        buttons.Add(cancel, 0);
        buttons.Add(save, 1);
        var content = new VerticalStackLayout { Padding = 20, Spacing = 14 };
        content.Add(new Label { Text = Title, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111814") });
        content.Add(new Label { Text = "Group", TextColor = Color.FromArgb("#26342c") });
        content.Add(groupPicker);
        if (names.Count == 0) content.Add(new Label { Text = "Add a group to a device first.", TextColor = Color.FromArgb("#26342c") });
        content.Add(new Label { Text = "Yaw", TextColor = Color.FromArgb("#26342c") });
        content.Add(yawEntry);
        content.Add(new Label { Text = "Pitch", TextColor = Color.FromArgb("#26342c") });
        content.Add(pitchEntry);
        content.Add(buttons);
        if (group is not null)
        {
            var delete = new Button { Text = "Delete", BackgroundColor = Color.FromArgb("#7A3434"), TextColor = Colors.White };
            delete.Clicked += OnDeleteClicked;
            content.Add(delete);
        }
        Content = new ScrollView { Content = new Border { Margin = 20, BackgroundColor = Color.FromArgb("#f9faf7"), Content = content } };
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (groupPicker.SelectedItem is not string name)
        {
            await DisplayAlertAsync("Missing details", "Choose a group.", "OK");
            return;
        }
        if (!float.TryParse(yawEntry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float yaw) ||
            !float.TryParse(pitchEntry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float pitch))
        {
            await DisplayAlertAsync("Invalid position", "Enter valid yaw and pitch values.", "OK");
            return;
        }
        GroupHotspot result = group ?? new GroupHotspot { SceneId = sceneId };
        result.GroupName = name;
        result.Yaw = yaw;
        result.Pitch = pitch;
        if (group is null) GroupCreated?.Invoke(this, result);
        else GroupUpdated?.Invoke(this, result);
        await Navigation.PopModalAsync();
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (group is not null && await DisplayAlertAsync("Delete group button", "Delete this group button?", "Delete", "Cancel"))
        {
            GroupDeleted?.Invoke(this, group);
            await Navigation.PopModalAsync();
        }
    }
}

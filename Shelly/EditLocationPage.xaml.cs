using System.Globalization;

namespace Shelly
{
    public partial class EditLocationPage : ContentPage
    {
        private readonly LocationHotspot locationHotspot;

        public EditLocationPage(IReadOnlyList<PanoramaScene> scenes, LocationHotspot locationHotspot)
        {
            InitializeComponent();

            this.locationHotspot = locationHotspot;

            List<PanoramaScene> targetScenes = scenes
                .Where(scene => scene.Id != locationHotspot.SourceSceneId)
                .ToList();

            TargetScenePicker.ItemsSource = targetScenes;
            TargetScenePicker.SelectedItem = targetScenes
                .FirstOrDefault(scene => scene.Id == locationHotspot.TargetSceneId);

            YawEntry.Text = locationHotspot.Yaw.ToString(CultureInfo.InvariantCulture);
            PitchEntry.Text = locationHotspot.Pitch.ToString(CultureInfo.InvariantCulture);
        }

        public event EventHandler<LocationHotspot>? LocationUpdated;

        public event EventHandler<LocationHotspot>? LocationDeleted;

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (TargetScenePicker.SelectedItem is not PanoramaScene targetScene)
            {
                await DisplayAlertAsync("Missing details", "Choose the target scene.", "OK");
                return;
            }

            if (!TryReadPosition(out float yaw, out float pitch))
            {
                await DisplayAlertAsync("Invalid position", "Enter valid yaw and pitch values.", "OK");
                return;
            }

            locationHotspot.TargetSceneId = targetScene.Id;
            locationHotspot.Yaw = yaw;
            locationHotspot.Pitch = pitch;

            LocationUpdated?.Invoke(this, locationHotspot);
            await Navigation.PopModalAsync();
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            bool shouldDelete = await DisplayAlertAsync(
                "Delete location",
                "Delete this location hotspot?",
                "Delete",
                "Cancel");

            if (!shouldDelete)
            {
                return;
            }

            LocationDeleted?.Invoke(this, locationHotspot);
            await Navigation.PopModalAsync();
        }

        private bool TryReadPosition(out float yaw, out float pitch)
        {
            bool hasYaw = float.TryParse(YawEntry.Text, out yaw);
            bool hasPitch = float.TryParse(PitchEntry.Text, out pitch);

            return hasYaw && hasPitch;
        }
    }
}

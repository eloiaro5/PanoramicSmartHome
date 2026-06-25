namespace Shelly
{
    public partial class AddLocationPage : ContentPage
    {
        private readonly Guid sourceSceneId;

        public AddLocationPage(IReadOnlyList<PanoramaScene> scenes, Guid sourceSceneId)
        {
            InitializeComponent();

            this.sourceSceneId = sourceSceneId;
            TargetScenePicker.ItemsSource = scenes.Where(scene => scene.Id != sourceSceneId).ToList();
        }

        public event EventHandler<LocationHotspot>? LocationCreated;

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

            LocationCreated?.Invoke(this, new LocationHotspot
            {
                SourceSceneId = sourceSceneId,
                TargetSceneId = targetScene.Id,
                Yaw = yaw,
                Pitch = pitch
            });

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

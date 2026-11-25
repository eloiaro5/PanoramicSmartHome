namespace Shelly
{
    public partial class AddPage : ContentPage
    {
        private readonly IReadOnlyList<PanoramaScene> scenes;
        private readonly IReadOnlyList<DeviceHotspot> devices;
        private readonly IReadOnlyList<DeviceHotspot> allDevices;
        private readonly Guid currentSceneId;
        private readonly PanoramaScene? currentScene;

        public AddPage(
            IReadOnlyList<PanoramaScene> scenes,
            IReadOnlyList<DeviceHotspot> devices,
            Guid currentSceneId,
            PanoramaScene? currentScene,
            IReadOnlyList<DeviceHotspot>? allDevices = null)
        {
            InitializeComponent();

            this.scenes = scenes;
            this.devices = devices;
            this.allDevices = allDevices ?? devices;
            this.currentSceneId = currentSceneId;
            this.currentScene = currentScene;
        }

        public event EventHandler<PanoramaScene>? SceneCreated;

        public event EventHandler<PanoramaScene>? SceneUpdated;

        public event EventHandler<PanoramaScene>? SceneDeleted;

        public event EventHandler<LocationHotspot>? LocationCreated;

        public event EventHandler<DeviceHotspot>? DeviceCreated;

        public event EventHandler<DeviceHotspot>? DeviceUpdated;

        public event EventHandler<DeviceHotspot>? DeviceDeleted;

        public event EventHandler<GroupHotspot>? GroupCreated;

        private async void OnAddGroupClicked(object? sender, EventArgs e)
        {
            var page = new GroupHotspotPage(allDevices, currentSceneId);
            page.GroupCreated += (_, group) =>
            {
                GroupCreated?.Invoke(this, group);
                _ = CloseMenuAfterChildClosesAsync();
            };
            await Navigation.PushModalAsync(page);
        }

        private async void OnAddSceneClicked(object? sender, EventArgs e)
        {
            var addScenePage = new AddScenePage(scenes);
            addScenePage.SceneCreated += (_, scene) =>
            {
                SceneCreated?.Invoke(this, scene);
                _ = CloseMenuAfterChildClosesAsync();
            };
            addScenePage.SceneUpdated += (_, scene) =>
            {
                SceneUpdated?.Invoke(this, scene);
                _ = CloseMenuAfterChildClosesAsync();
            };
            addScenePage.SceneDeleted += (_, scene) =>
            {
                SceneDeleted?.Invoke(this, scene);
                _ = CloseMenuAfterChildClosesAsync();
            };

            await Navigation.PushModalAsync(addScenePage);
        }

        private async void OnAddLocationClicked(object? sender, EventArgs e)
        {
            var addLocationPage = new AddLocationPage(scenes, currentSceneId);
            addLocationPage.LocationCreated += (_, locationHotspot) =>
            {
                LocationCreated?.Invoke(this, locationHotspot);
                _ = CloseMenuAfterChildClosesAsync();
            };

            await Navigation.PushModalAsync(addLocationPage);
        }

        private async void OnAddDeviceClicked(object? sender, EventArgs e)
        {
            var addSwitchPage = new AddSwitchPage(devices);
            addSwitchPage.SwitchCreated += (_, deviceHotspot) =>
            {
                DeviceCreated?.Invoke(this, deviceHotspot);
                _ = CloseMenuAfterChildClosesAsync();
            };
            addSwitchPage.SwitchUpdated += (_, deviceHotspot) =>
            {
                DeviceUpdated?.Invoke(this, deviceHotspot);
                _ = CloseMenuAfterChildClosesAsync();
            };
            addSwitchPage.SwitchDeleted += (_, deviceHotspot) =>
            {
                DeviceDeleted?.Invoke(this, deviceHotspot);
                _ = CloseMenuAfterChildClosesAsync();
            };

            await Navigation.PushModalAsync(addSwitchPage);
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async Task CloseMenuAfterChildClosesAsync()
        {
            await Task.Delay(150);

            if (Navigation.ModalStack.Contains(this))
            {
                await Navigation.PopModalAsync();
            }
        }
    }
}

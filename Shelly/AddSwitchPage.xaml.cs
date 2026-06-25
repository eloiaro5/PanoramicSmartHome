using System.Globalization;

namespace Shelly
{
    public partial class AddSwitchPage : ContentPage
    {
        private readonly IReadOnlyList<DeviceHotspot> switches;
        private DeviceHotspot? loadedSwitch;

        public AddSwitchPage()
            : this([])
        {
        }

        public AddSwitchPage(IReadOnlyList<DeviceHotspot> switches, DeviceHotspot? deviceHotspot = null)
        {
            InitializeComponent();

            this.switches = switches;

            if (deviceHotspot is not null)
            {
                LoadSwitch(deviceHotspot);
            }
        }

        public event EventHandler<DeviceHotspot>? SwitchCreated;

        public event EventHandler<DeviceHotspot>? SwitchUpdated;

        public event EventHandler<DeviceHotspot>? SwitchDeleted;

        private void OnNameCompleted(object? sender, EventArgs e)
        {
            TryLoadSwitchByName();
        }

        private void OnNameUnfocused(object? sender, FocusEventArgs e)
        {
            TryLoadSwitchByName();
        }

        private void TryLoadSwitchByName()
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            DeviceHotspot? deviceHotspot = switches.FirstOrDefault(device =>
                string.Equals(device.Name, name, StringComparison.OrdinalIgnoreCase));

            if (deviceHotspot is not null)
            {
                LoadSwitch(deviceHotspot);
            }
        }

        private void LoadSwitch(DeviceHotspot deviceHotspot)
        {
            loadedSwitch = deviceHotspot;
            TitleLabel.Text = "Edit switch";
            NameEntry.Text = deviceHotspot.Name;
            IpAddressEntry.Text = deviceHotspot.IpAddress;
            YawEntry.Text = deviceHotspot.Yaw.ToString(CultureInfo.InvariantCulture);
            PitchEntry.Text = deviceHotspot.Pitch.ToString(CultureInfo.InvariantCulture);
            DeleteButton.IsVisible = true;
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;
            string ipAddress = IpAddressEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ipAddress))
            {
                await DisplayAlertAsync("Missing details", "Enter both a name and an IP address.", "OK");
                return;
            }

            if (!TryReadPosition(out float yaw, out float pitch))
            {
                await DisplayAlertAsync("Invalid position", "Enter valid yaw and pitch values.", "OK");
                return;
            }

            try
            {
                _ = new ShellySwitch(name, ipAddress);
            }
            catch (ArgumentException ex)
            {
                await DisplayAlertAsync("Invalid switch", ex.Message, "OK");
                return;
            }

            if (loadedSwitch is null)
            {
                SwitchCreated?.Invoke(this, new DeviceHotspot
                {
                    Name = name,
                    IpAddress = ipAddress,
                    Yaw = yaw,
                    Pitch = pitch,
                    CurrentState = DeviceState.Waiting
                });
                await Navigation.PopModalAsync();
                return;
            }

            loadedSwitch.Name = name;
            loadedSwitch.IpAddress = ipAddress;
            loadedSwitch.Yaw = yaw;
            loadedSwitch.Pitch = pitch;

            SwitchUpdated?.Invoke(this, loadedSwitch);
            await Navigation.PopModalAsync();
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (loadedSwitch is null)
            {
                return;
            }

            bool shouldDelete = await DisplayAlertAsync(
                "Delete switch",
                "Delete this switch hotspot?",
                "Delete",
                "Cancel");

            if (!shouldDelete)
            {
                return;
            }

            SwitchDeleted?.Invoke(this, loadedSwitch);
            await Navigation.PopModalAsync();
        }

        private bool TryReadPosition(out float yaw, out float pitch)
        {
            bool hasYaw = float.TryParse(
                YawEntry.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out yaw);
            bool hasPitch = float.TryParse(
                PitchEntry.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out pitch);

            return hasYaw && hasPitch;
        }
    }
}

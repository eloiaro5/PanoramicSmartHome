using System.Text.Json;
using System.Globalization;
using Microsoft.Maui.Layouts;

namespace Shelly
{
    public partial class MainPage : ContentPage
    {
        private const double SwitchButtonSize = 52;
        private const double LocationButtonSize = 44;
        private static readonly TimeSpan LongPressDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DeviceResponseTimeout = TimeSpan.FromSeconds(5);
        private readonly HouseDataRepository repository = new();
        private readonly Dictionary<DeviceHotspot, Button> hotspotViews = [];
        private readonly Dictionary<LocationHotspot, Button> locationViews = [];
        private readonly List<DeviceHotspot> hotspots = [];
        private readonly List<LocationHotspot> locationHotspots = [];
        private readonly List<PanoramaScene> scenes = [];
        private PanoramaScene? currentScene;
        private Guid? lastSceneId;
        private float lastYaw;
        private float lastPitch;

        public MainPage()
        {
            InitializeComponent();
            PanoramaWebView.Navigating += OnPanoramaWebViewNavigating;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            Loaded -= OnPageLoaded;

            await LoadPersistedDataAsync();

            if (scenes.Count == 0)
            {
                await OpenInitialAddScenePageAsync();
                return;
            }

            PanoramaScene startupScene = scenes.FirstOrDefault(scene => scene.Id == lastSceneId) ?? scenes[0];
            await LoadPanoramaSceneAsync(startupScene);
        }

        private async Task OpenInitialAddScenePageAsync()
        {
            var addScenePage = new AddScenePage();
            addScenePage.SceneCreated += async (_, scene) =>
            {
                scenes.Add(scene);
                await SaveDataAsync();
                await LoadPanoramaSceneAsync(scene);
            };

            await Navigation.PushModalAsync(addScenePage);
        }

        private async Task LoadPanoramaSceneAsync(PanoramaScene scene)
        {
            currentScene = scene;
            lastSceneId = scene.Id;

            await using Stream imageStream = await OpenSceneImageAsync(scene.ImagePath);
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);

            string base64Image = Convert.ToBase64String(memoryStream.ToArray());

            List<DeviceHotspot> sceneDevices = hotspots
                .Where(hotspot => hotspot.SceneId == scene.Id)
                .ToList();
            List<LocationHotspot> sceneLocations = locationHotspots
                .Where(location => location.SourceSceneId == scene.Id)
                .ToList();

            PanoramaWebView.Source = new HtmlWebViewSource
            {
                Html = CreatePanoramaTestHtml(base64Image, scene.Name, sceneDevices, sceneLocations, lastYaw, lastPitch)
            };

            _ = SaveDataAsync();
        }

        private static string CreatePanoramaTestHtml(
            string base64Image,
            string sceneName,
            IReadOnlyCollection<DeviceHotspot> deviceHotspots,
            IReadOnlyCollection<LocationHotspot> locationHotspots,
            float initialYaw,
            float initialPitch)
        {
            string hotspotsJson = JsonSerializer.Serialize(
                BuildHtmlHotspots(deviceHotspots, locationHotspots),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            string encodedSceneName = System.Net.WebUtility.HtmlEncode(sceneName);
            string initialYawText = initialYaw.ToString(CultureInfo.InvariantCulture);
            string initialPitchText = initialPitch.ToString(CultureInfo.InvariantCulture);

            return $$"""
<!doctype html>
<html>
<head>
  <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0">
  <style>
    html, body, #viewer {
      width: 100%;
      height: 100%;
      margin: 0;
      overflow: hidden;
      background: #111814;
    }

    #viewer {
      width: 100%;
      height: 100%;
      overflow: hidden;
      position: relative;
      touch-action: none;
    }

    #stage {
      height: 100%;
      width: auto;
      max-width: none;
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
      user-select: none;
    }

    #panorama {
      height: 100%;
      width: auto;
      max-width: none;
      display: block;
      -webkit-user-drag: none;
    }

    .hotspot {
      position: absolute;
      transform: translate(-50%, -50%);
      z-index: 4;
      display: grid;
      place-items: center;
      border: 2px solid #fff;
      font-weight: 700;
      user-select: none;
      touch-action: none;
    }

    .device-hotspot {
      width: 52px;
      height: 52px;
      border-radius: 999px;
      color: var(--text);
      background: var(--background);
    }

    .location-hotspot {
      width: 44px;
      height: 44px;
      border-radius: 0;
      border-color: #26342c;
      color: #000;
      background: #fff;
      font-size: 22px;
    }

    .controls {
      position: absolute;
      left: 50%;
      bottom: 18px;
      transform: translateX(-50%);
      display: grid;
      grid-template-columns: 48px 48px;
      gap: 8px;
      z-index: 5;
    }

    .control {
      border: 1px solid rgba(255,255,255,0.72);
      border-radius: 8px;
      background: rgba(17,24,20,0.78);
      color: #fff;
      font-size: 24px;
      font-weight: 700;
      line-height: 1;
    }

    .control:active {
      background: rgba(57,67,61,0.92);
    }

  </style>
</head>
<body>
  <main id="viewer">
    <section id="stage">
      <img id="panorama" src="data:image/jpeg;base64,{{base64Image}}" alt="{{encodedSceneName}} panorama" draggable="false">
    </section>
    <nav class="controls" aria-label="Panorama controls">
      <button class="control left" type="button" data-pan-x="1">←</button>
      <button class="control right" type="button" data-pan-x="-1">→</button>
    </nav>
  </main>
  <script>
    const viewer = document.getElementById('viewer');
    const stage = document.getElementById('stage');
    const panorama = document.getElementById('panorama');
    const hotspots = {{hotspotsJson}};
    const step = 120;
    const initialYaw = {{initialYawText}};
    const initialPitch = {{initialPitchText}};
    let offsetX = 0;
    let offsetY = 0;

    function clamp(value, min, max) {
      return Math.min(Math.max(value, min), max);
    }

    function getBounds() {
      const viewerRect = viewer.getBoundingClientRect();
      const imageRect = panorama.getBoundingClientRect();
      const maxY = Math.max(0, (imageRect.height - viewerRect.height) / 2);
      return { maxY };
    }

    function applyPan() {
      const bounds = getBounds();
      offsetY = clamp(offsetY, -bounds.maxY, bounds.maxY);
      stage.style.transform = `translate(calc(-50% + ${offsetX}px), calc(-50% + ${offsetY}px))`;
    }

    function wrapHorizontalOffset() {
      const imageRect = panorama.getBoundingClientRect();

      if (imageRect.width <= 0) {
        return;
      }

      const halfWidth = imageRect.width / 2;

      while (offsetX > halfWidth) {
        offsetX -= imageRect.width;
      }

      while (offsetX < -halfWidth) {
        offsetX += imageRect.width;
      }
    }

    function yawToPercent(yaw) {
      return ((((yaw + 180) % 360) + 360) % 360) / 360;
    }

    function getCurrentYaw() {
      const imageRect = panorama.getBoundingClientRect();

      if (imageRect.width <= 0) {
        return initialYaw;
      }

      const percent = (imageRect.width / 2 - offsetX) / imageRect.width;
      return percent * 360 - 180;
    }

    function reportView() {
      window.location.href = `shelly://view/${getCurrentYaw().toFixed(4)}/${initialPitch.toFixed(4)}`;
    }

    function applyInitialView() {
      const imageRect = panorama.getBoundingClientRect();
      offsetX = imageRect.width / 2 - yawToPercent(initialYaw) * imageRect.width;
      offsetY = 0;
      applyPan();
    }

    function pan(deltaX, deltaY) {
      offsetX += deltaX;
      offsetY += deltaY;
      wrapHorizontalOffset();
      applyPan();
      reportView();
    }

    document.querySelectorAll('.control').forEach(button => {
      button.addEventListener('click', () => {
        pan(
          Number(button.dataset.panX || 0) * step,
          Number(button.dataset.panY || 0) * step);
      });
    });

    function addHotspot(hotspot) {
      const element = document.createElement('button');
      element.type = 'button';
      element.className = `hotspot ${hotspot.kind}-hotspot`;
      element.textContent = hotspot.text;
      element.style.left = `${hotspot.x}%`;
      element.style.top = `${hotspot.y}%`;

      if (hotspot.kind === 'device') {
        element.style.setProperty('--background', hotspot.background);
        element.style.setProperty('--text', hotspot.textColor);
      }

      let longPressTimer = null;
      let longPressHandled = false;

      element.addEventListener('pointerdown', event => {
        event.preventDefault();
        longPressHandled = false;
        longPressTimer = window.setTimeout(() => {
          longPressHandled = true;
          window.location.href = `shelly://${hotspot.kind}/${hotspot.id}/long`;
        }, 1000);
      });

      element.addEventListener('pointerup', event => {
        event.preventDefault();
        window.clearTimeout(longPressTimer);

        if (!longPressHandled) {
          window.location.href = `shelly://${hotspot.kind}/${hotspot.id}/tap`;
        }
      });

      element.addEventListener('pointercancel', () => {
        window.clearTimeout(longPressTimer);
      });

      stage.appendChild(element);
    }

    hotspots.forEach(addHotspot);
    window.addEventListener('resize', applyPan);
    panorama.addEventListener('load', applyInitialView);
  </script>
</body>
</html>
""";
        }

        private static IEnumerable<HtmlHotspot> BuildHtmlHotspots(
            IReadOnlyCollection<DeviceHotspot> deviceHotspots,
            IReadOnlyCollection<LocationHotspot> locationHotspots)
        {
            foreach (DeviceHotspot hotspot in deviceHotspots)
            {
                yield return new HtmlHotspot(
                    "device",
                    hotspot.Id,
                    YawToPercent(hotspot.Yaw),
                    PitchToPercent(hotspot.Pitch),
                    GetHotspotLabel(hotspot.Name),
                    GetDeviceStateBackgroundHex(hotspot.CurrentState),
                    GetDeviceStateTextHex(hotspot.CurrentState));
            }

            foreach (LocationHotspot locationHotspot in locationHotspots)
            {
                yield return new HtmlHotspot(
                    "location",
                    locationHotspot.Id,
                    YawToPercent(locationHotspot.Yaw),
                    PitchToPercent(locationHotspot.Pitch),
                    "X",
                    "#ffffff",
                    "#000000");
            }
        }

        private static string CreateEmptySceneHtml()
        {
            return """
<!doctype html>
<html>
<head>
  <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0">
  <style>
    html, body {
      width: 100%;
      height: 100%;
      margin: 0;
      display: grid;
      place-items: center;
      background: #111814;
      color: #f5f6f3;
      font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }
  </style>
</head>
<body>No scene</body>
</html>
""";
        }

        private async void OnAddTapped(object? sender, TappedEventArgs e)
        {
            if (currentScene is null)
            {
                return;
            }

            List<DeviceHotspot> sceneDevices = hotspots
                .Where(hotspot => hotspot.SceneId == currentScene.Id)
                .ToList();

            var addPage = new AddPage(scenes, sceneDevices, currentScene.Id, currentScene);
            addPage.SceneCreated += OnSceneCreated;
            addPage.SceneUpdated += OnSceneUpdated;
            addPage.SceneDeleted += OnSceneDeleted;
            addPage.LocationCreated += OnLocationCreated;
            addPage.DeviceCreated += OnHotspotCreated;
            addPage.DeviceUpdated += OnHotspotUpdated;
            addPage.DeviceDeleted += OnHotspotDeleted;

            await Navigation.PushModalAsync(addPage);
        }

        private async void OnPanoramaWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!Uri.TryCreate(e.Url, UriKind.Absolute, out Uri? uri) ||
                !string.Equals(uri.Scheme, "shelly", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            e.Cancel = true;

            string kind = uri.Host;
            string[] segments = uri.AbsolutePath
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (kind == "view")
            {
                if (segments.Length != 2 ||
                    !float.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float yaw) ||
                    !float.TryParse(segments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float pitch))
                {
                    return;
                }

                lastYaw = yaw;
                lastPitch = pitch;
                _ = SaveDataAsync();
                return;
            }

            if (segments.Length != 2 || !Guid.TryParse(segments[0], out Guid hotspotId))
            {
                return;
            }

            string action = segments[1];

            if (kind == "device")
            {
                DeviceHotspot? hotspot = hotspots.SingleOrDefault(item => item.Id == hotspotId);

                if (hotspot is null)
                {
                    return;
                }

                if (action == "tap")
                {
                    await ToggleDeviceAsync(hotspot);
                }
                else if (action == "long")
                {
                    await OpenEditHotspotPageAsync(hotspot);
                }
            }
            else if (kind == "location")
            {
                LocationHotspot? locationHotspot = locationHotspots.SingleOrDefault(item => item.Id == hotspotId);

                if (locationHotspot is null)
                {
                    return;
                }

                if (action == "tap")
                {
                    await NavigateToLocationAsync(locationHotspot);
                }
                else if (action == "long")
                {
                    await OpenEditLocationPageAsync(locationHotspot);
                }
            }
        }

        private void OnHotspotCreated(object? sender, DeviceHotspot hotspot)
        {
            if (currentScene is not null)
            {
                hotspot.SceneId = currentScene.Id;
            }

            hotspots.Add(hotspot);
            ReloadCurrentScene();
            _ = SaveDataAsync();
            _ = ReadInitialDeviceStatusAsync(hotspot);
        }

        private void OnLocationCreated(object? sender, LocationHotspot locationHotspot)
        {
            locationHotspots.Add(locationHotspot);
            ReloadCurrentScene();
            _ = SaveDataAsync();
        }

        private void OnSceneCreated(object? sender, PanoramaScene scene)
        {
            scenes.Add(scene);
            _ = SaveDataAsync();

            if (currentScene is null)
            {
                _ = LoadPanoramaSceneAsync(scene);
            }
        }

        private void OnSceneUpdated(object? sender, PanoramaScene scene)
        {
            _ = SaveDataAsync();

            if (currentScene?.Id == scene.Id)
            {
                _ = LoadPanoramaSceneAsync(scene);
            }
        }

        private void OnSceneDeleted(object? sender, PanoramaScene scene)
        {
            scenes.Remove(scene);
            hotspots.RemoveAll(hotspot => hotspot.SceneId == scene.Id);
            locationHotspots.RemoveAll(location =>
                location.SourceSceneId == scene.Id ||
                location.TargetSceneId == scene.Id);

            AddScenePage.DeleteSceneImageIfOwned(scene.ImagePath);

            if (currentScene?.Id == scene.Id)
            {
                currentScene = null;
                lastSceneId = null;
                lastYaw = 0;
                lastPitch = 0;
            }

            _ = SaveAndLoadAfterSceneDeletedAsync();
        }

        private async Task SaveAndLoadAfterSceneDeletedAsync()
        {
            await SaveDataAsync();

            if (scenes.Count == 0)
            {
                PanoramaWebView.Source = new HtmlWebViewSource
                {
                    Html = CreateEmptySceneHtml()
                };
                await OpenInitialAddScenePageAsync();
                return;
            }

            await LoadPanoramaSceneAsync(scenes[0]);
        }

        private void AddHotspotToCanvas(DeviceHotspot hotspot)
        {
            var buttonView = new Button
            {
                WidthRequest = SwitchButtonSize,
                HeightRequest = SwitchButtonSize,
                BorderColor = Colors.White,
                BorderWidth = 2,
                CornerRadius = (int)(SwitchButtonSize / 2),
                BindingContext = hotspot,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                Text = GetHotspotLabel(hotspot.Name)
            };

            UpdateButtonState(hotspot, buttonView);
            SetHotspotLayout(buttonView, hotspot);
            AddHotspotPressHandlers(buttonView, hotspot);

            SwitchLayer.Children.Add(buttonView);
            hotspotViews[hotspot] = buttonView;
        }

        private void AddLocationToCanvas(LocationHotspot locationHotspot)
        {
            var buttonView = new Button
            {
                WidthRequest = LocationButtonSize,
                HeightRequest = LocationButtonSize,
                BorderColor = Color.FromArgb("#26342c"),
                BorderWidth = 1,
                CornerRadius = 0,
                BindingContext = locationHotspot,
                BackgroundColor = Colors.White,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                FontSize = 22,
                Text = "X"
            };

            SetLocationLayout(buttonView, locationHotspot);
            AddLocationPressHandlers(buttonView, locationHotspot);

            SwitchLayer.Children.Add(buttonView);
            locationViews[locationHotspot] = buttonView;
        }

        private static string GetHotspotLabel(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "D" : name[..1].ToUpperInvariant();
        }

        private static void SetHotspotLayout(View view, DeviceHotspot hotspot)
        {
            AbsoluteLayout.SetLayoutBounds(
                view,
                new Rect(
                    YawToProportionalX(hotspot.Yaw),
                    PitchToProportionalY(hotspot.Pitch),
                    SwitchButtonSize,
                    SwitchButtonSize));
            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.PositionProportional);
        }

        private static void SetLocationLayout(View view, LocationHotspot locationHotspot)
        {
            AbsoluteLayout.SetLayoutBounds(
                view,
                new Rect(
                    YawToProportionalX(locationHotspot.Yaw),
                    PitchToProportionalY(locationHotspot.Pitch),
                    LocationButtonSize,
                    LocationButtonSize));
            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.PositionProportional);
        }

        private async Task NavigateToLocationAsync(LocationHotspot locationHotspot)
        {
            PanoramaScene? targetScene = scenes.SingleOrDefault(scene => scene.Id == locationHotspot.TargetSceneId);

            if (targetScene is null)
            {
                await DisplayAlertAsync("Location error", "The target scene was not found.", "OK");
                return;
            }

            lastYaw = 0;
            lastPitch = 0;
            await LoadPanoramaSceneAsync(targetScene);
        }

        private void RefreshVisibleHotspots()
        {
            SwitchLayer.Children.Clear();
            hotspotViews.Clear();
            locationViews.Clear();
        }

        private void AddHotspotPressHandlers(Button view, DeviceHotspot hotspot)
        {
            CancellationTokenSource? longPressTokenSource = null;
            bool longPressHandled = false;

            view.Pressed += (_, _) =>
            {
                longPressHandled = false;
                longPressTokenSource?.Cancel();
                longPressTokenSource = new CancellationTokenSource();
                _ = OpenEditAfterLongPressAsync(hotspot, longPressTokenSource.Token, () => longPressHandled = true);
            };

            view.Released += async (_, _) =>
            {
                longPressTokenSource?.Cancel();

                if (!longPressHandled)
                {
                    await ToggleDeviceAsync(hotspot);
                }
            };
        }

        private void AddLocationPressHandlers(Button view, LocationHotspot locationHotspot)
        {
            CancellationTokenSource? longPressTokenSource = null;
            bool longPressHandled = false;

            view.Pressed += (_, _) =>
            {
                longPressHandled = false;
                longPressTokenSource?.Cancel();
                longPressTokenSource = new CancellationTokenSource();
                _ = OpenEditLocationAfterLongPressAsync(
                    locationHotspot,
                    longPressTokenSource.Token,
                    () => longPressHandled = true);
            };

            view.Released += async (_, _) =>
            {
                longPressTokenSource?.Cancel();

                if (!longPressHandled)
                {
                    await NavigateToLocationAsync(locationHotspot);
                }
            };
        }

        private async Task OpenEditLocationAfterLongPressAsync(
            LocationHotspot locationHotspot,
            CancellationToken cancellationToken,
            Action markHandled)
        {
            try
            {
                await Task.Delay(LongPressDelay, cancellationToken);
                markHandled();
                await MainThread.InvokeOnMainThreadAsync(() => OpenEditLocationPageAsync(locationHotspot));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task OpenEditAfterLongPressAsync(
            DeviceHotspot hotspot,
            CancellationToken cancellationToken,
            Action markHandled)
        {
            try
            {
                await Task.Delay(LongPressDelay, cancellationToken);
                markHandled();
                await MainThread.InvokeOnMainThreadAsync(() => OpenEditHotspotPageAsync(hotspot));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ToggleDeviceAsync(DeviceHotspot hotspot)
        {
            try
            {
                bool turnOn = hotspot.CurrentState != DeviceState.On;
                await SendDeviceCommandAsync(hotspot, turnOn);
            }
            catch (Exception ex)
            {
            hotspot.CurrentState = DeviceState.Failed;
                ReloadCurrentScene();
                _ = SaveDataAsync();
                await DisplayAlertAsync("Switch error", ex.Message, "OK");
            }
        }

        private async Task OpenEditHotspotPageAsync(DeviceHotspot hotspot)
        {
            List<DeviceHotspot> sceneDevices = hotspots
                .Where(item => item.SceneId == hotspot.SceneId)
                .ToList();

            var editSwitchPage = new AddSwitchPage(sceneDevices, hotspot);
            editSwitchPage.SwitchUpdated += OnHotspotUpdated;
            editSwitchPage.SwitchDeleted += OnHotspotDeleted;

            await Navigation.PushModalAsync(editSwitchPage);
        }

        private async Task OpenEditLocationPageAsync(LocationHotspot locationHotspot)
        {
            var editLocationPage = new EditLocationPage(scenes, locationHotspot);
            editLocationPage.LocationUpdated += OnLocationUpdated;
            editLocationPage.LocationDeleted += OnLocationDeleted;

            await Navigation.PushModalAsync(editLocationPage);
        }

        private void OnLocationUpdated(object? sender, LocationHotspot locationHotspot)
        {
            ReloadCurrentScene();
            _ = SaveDataAsync();
        }

        private void OnLocationDeleted(object? sender, LocationHotspot locationHotspot)
        {
            locationHotspots.Remove(locationHotspot);
            ReloadCurrentScene();
            _ = SaveDataAsync();
        }

        private void OnHotspotUpdated(object? sender, DeviceHotspot hotspot)
        {
            ReloadCurrentScene();
            _ = SaveDataAsync();
        }

        private void OnHotspotDeleted(object? sender, DeviceHotspot hotspot)
        {
            hotspots.Remove(hotspot);
            ReloadCurrentScene();
            _ = SaveDataAsync();
        }

        private async Task ReadInitialDeviceStatusAsync(DeviceHotspot hotspot)
        {
            try
            {
                await ReadDeviceStatusAsync(hotspot);
            }
            catch (Exception)
            {
                hotspot.CurrentState = DeviceState.Failed;
                ReloadCurrentScene();
                _ = SaveDataAsync();
            }
        }

        private async Task ReadDeviceStatusAsync(DeviceHotspot hotspot)
        {
            hotspot.CurrentState = DeviceState.Waiting;
            ReloadCurrentScene();

            using var timeoutTokenSource = new CancellationTokenSource(DeviceResponseTimeout);
            using var status = await new ShellySwitch(hotspot.Name, hotspot.IpAddress)
                .ReadStatusAsync(timeoutTokenSource.Token);

            bool isOn = status.RootElement.TryGetProperty("output", out var output)
                && output.ValueKind == System.Text.Json.JsonValueKind.True;

            hotspot.CurrentState = isOn ? DeviceState.On : DeviceState.Off;
            ReloadCurrentScene();
            await SaveDataAsync();
        }

        private async Task SendDeviceCommandAsync(DeviceHotspot hotspot, bool turnOn)
        {
            hotspot.CurrentState = DeviceState.Waiting;
            ReloadCurrentScene();

            using var timeoutTokenSource = new CancellationTokenSource(DeviceResponseTimeout);
            var shellySwitch = new ShellySwitch(hotspot.Name, hotspot.IpAddress);

            if (turnOn)
            {
                await shellySwitch.TurnOnAsync(timeoutTokenSource.Token);
                hotspot.CurrentState = DeviceState.On;
            }
            else
            {
                await shellySwitch.TurnOffAsync(timeoutTokenSource.Token);
                hotspot.CurrentState = DeviceState.Off;
            }

            ReloadCurrentScene();
            await SaveDataAsync();
        }

        private void UpdateButtonState(DeviceHotspot hotspot)
        {
            if (hotspotViews.TryGetValue(hotspot, out Button? buttonView))
            {
                UpdateButtonState(hotspot, buttonView);
            }
        }

        private static void UpdateButtonState(DeviceHotspot hotspot, Button buttonView)
        {
            buttonView.BackgroundColor = hotspot.CurrentState switch
            {
                DeviceState.On => Colors.Green,
                DeviceState.Off => Colors.Red,
                DeviceState.Failed => Color.FromArgb("#4A4A4A"),
                _ => Color.FromArgb("#F5F5F5")
            };

            buttonView.TextColor = hotspot.CurrentState switch
            {
                DeviceState.On => Color.FromArgb("#E6F0D8"),
                DeviceState.Off => Color.FromArgb("#F7DDD2"),
                DeviceState.Failed => Color.FromArgb("#F5F5F5"),
                _ => Color.FromArgb("#4A4A4A")
            };
        }

        private static double YawToProportionalX(float yaw)
        {
            double normalizedYaw = ((yaw + 180) % 360 + 360) % 360;
            return normalizedYaw / 360;
        }

        private static double YawToPercent(float yaw)
        {
            return YawToProportionalX(yaw) * 100;
        }

        private static double PitchToProportionalY(float pitch)
        {
            return Math.Clamp((90 - pitch) / 180, 0, 1);
        }

        private static double PitchToPercent(float pitch)
        {
            return PitchToProportionalY(pitch) * 100;
        }

        private static string GetDeviceStateBackgroundHex(DeviceState state)
        {
            return state switch
            {
                DeviceState.On => "#008000",
                DeviceState.Off => "#ff0000",
                DeviceState.Failed => "#4A4A4A",
                _ => "#F5F5F5"
            };
        }

        private static string GetDeviceStateTextHex(DeviceState state)
        {
            return state switch
            {
                DeviceState.On => "#E6F0D8",
                DeviceState.Off => "#F7DDD2",
                DeviceState.Failed => "#F5F5F5",
                _ => "#4A4A4A"
            };
        }

        private static Task<Stream> OpenSceneImageAsync(string imagePath)
        {
            if (Path.IsPathRooted(imagePath))
            {
                return Task.FromResult<Stream>(File.OpenRead(imagePath));
            }

            return FileSystem.OpenAppPackageFileAsync(imagePath);
        }

        private async Task LoadPersistedDataAsync()
        {
            HouseData data = await repository.LoadAsync();

            scenes.Clear();
            scenes.AddRange(data.Scenes);

            lastSceneId = data.LastSceneId;
            lastYaw = data.LastYaw;
            lastPitch = data.LastPitch;

            hotspots.Clear();
            hotspots.AddRange(data.DeviceHotspots);

            locationHotspots.Clear();
            locationHotspots.AddRange(data.LocationHotspots);
        }

        private Task SaveDataAsync()
        {
            return repository.SaveAsync(new HouseData
            {
                LastSceneId = lastSceneId,
                LastYaw = lastYaw,
                LastPitch = lastPitch,
                Scenes = [.. scenes],
                DeviceHotspots = [.. hotspots],
                LocationHotspots = [.. locationHotspots]
            });
        }

        private void ReloadCurrentScene()
        {
            if (currentScene is not null)
            {
                _ = LoadPanoramaSceneAsync(currentScene);
            }
        }

        private sealed record HtmlHotspot(
            string Kind,
            Guid Id,
            double X,
            double Y,
            string Text,
            string Background,
            string TextColor);
    }
}

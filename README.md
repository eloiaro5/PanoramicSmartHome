# Shelly

Shelly is a .NET MAUI smart-home app for managing Shelly switches inside panoramic room scenes.

The current app lets you:

- Create panoramic scenes from local images.
- Add Shelly device hotspots with a name, IP address, yaw, and pitch.
- Assign optional group names to each device in the device editor.
- Add group buttons to a scene to control devices sharing a group name, including devices in other scenes. A tap turns all members off if they are all on, or on otherwise; a long press edits or deletes the button.
- Double-press a device or group button to open a full-screen switch for it; use the map icon to return to the scene.
- Add location hotspots to move between scenes.
- Persist scenes, hotspots, and the last visited camera position in local JSON storage.
- Toggle Shelly switches through HTTPS calls and show their current state in the panorama.

## Requirements

- .NET 10 SDK
- .NET MAUI workload
- Visual Studio with MAUI support, or the .NET CLI

## Build

From the repository root:

```powershell
dotnet build Shelly\Shelly.csproj
```

## Project Layout

- `Shelly/` - MAUI application source.
- `Shelly/Resources/` - app resources and bundled assets.
- `Shelly/HouseDataRepository.cs` - JSON persistence for scenes, devices, locations, and last view state.
- `Shelly/PanoramaScene.cs` - scene model.
- `Shelly/DeviceHotspot.cs` - Shelly device hotspot model.
- `Shelly/LocationHotspot.cs` - navigation hotspot model.
- `Shelly/ShellySwitch.cs` - HTTPS client wrapper for Shelly switch calls.

## Notes

Scene images selected inside the app are copied into the app data directory under `Resources/Images/Scenes`. They are not committed to the repository unless manually added as bundled assets.

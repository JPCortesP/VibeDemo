# Airspace

Airspace is a WinUI 3 flight-traffic dashboard prototype. It starts with a clearly
identified synthetic snapshot, then attempts to load a regional snapshot from the
public [ADSB.lol API](https://www.adsb.lol/). The request is centered near
39.5° N, 98.35° W and is limited to a configurable radius of 25-250 nautical
miles. This is not global coverage or a complete census of aircraft.

The app shows callsigns, aircraft types and other fields only when the feed
provides them. Callsign prefixes are grouped as prefixes, not presented as
verified airline identities. If the live request fails or returns no aircraft,
the app keeps a randomized sample visible, labels it as synthetic, and explains
the fallback. Turning on Demo mode uses generated data without making a network
request. The app does not require credentials or an API key.

## Run

Requires Windows, the .NET SDK for the project target, WinApp CLI 0.7 or newer,
and Developer Mode enabled for local packaged deployment. From the project
directory:

```powershell
winapp run . --detach --json
```

The app is packaged by default and requires network access for live snapshots.

## WinUI skills

The project guidance uses these installed skill identifiers:

- `winui-design` for Fluent layout, native-control selection, theming, and
  accessibility.
- `winui-dev-workflow` for packaged WinUI builds and launch.
- `winui-code-review` for MVVM, binding, accessibility, and performance review.
- `winui-ui-testing` for Windows UI Automation and visual verification.

The app uses the WinUI MVVM template, CommunityToolkit.Mvvm source generators,
compiled `x:Bind` bindings, an app-wide `NavigationView`, and the Windows SDK
WinUI analyzer.

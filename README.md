# VNO.Client

Desktop player client for Visual Novel Online, built with .NET 10 and Avalonia 12.

## Overview

`VNO.Client` is the player-facing desktop application for the Visual Novel Online stack. It connects to:

- `VNO.Master` for account login, version checks, news and the public server list
- `VNO.Server` for in-scene chat, areas, music, roster selection, moderation events and other live gameplay traffic

This port preserves the original VNO content model. Themes, roster art, character sprites, backgrounds, sounds and INI-driven UI settings are loaded from a runtime `data/` folder instead of being embedded into the application assembly. However, this might change as time goes by.

## Project Status

This project is currently under active development. It is a modern .NET and Avalonia port of the legacy VNO desktop client, with the original asset layout, theme system and protocol conventions intentionally preserved.

## Features

Current features include:

- Avalonia desktop GUI for login, server selection, character selection and the game stage
- Shared protocol layer through the `external/VNO.Core` submodule
- Master Server login, version-check and server-list integration
- Direct game server connection support
- Legacy-style theme loading from `data/UI/<design>/design.ini`
- Character roster, emote, background and big-art loading from the legacy asset tree
- INI-driven settings and theme helpers
- BASS-backed audio with graceful fallback when the native BASS library is missing (this will change soon)
- Replay behaviour support
- Moderator tooling support
- Automated tests for protocol-facing services, asset loaders, replay behaviour and moderator flows

## Repository Layout

```text id="2xbeve"
src/
  VNO.Client/          Application entry point, views, view models and services

tests/
  VNO.Client.Tests/    Client-focused unit tests

external/
  VNO.Core/            Shared protocol and networking submodule
```

## Requirements

- .NET 10 SDK
- Git with submodule support
- A runtime `data/` folder containing the legacy VNO assets
- Optional: native BASS runtime if you want music and sound playback

## Installation

Clone the repository with submodules:

```bash id="0k9zgd"
git clone --recurse-submodules https://github.com/Lemonical/VNO.Client.git
cd VNO.Client
dotnet restore VNO.Client.slnx
```

If you already cloned the repository without submodules:

```bash id="x9jbi1"
git submodule update --init --recursive
```

## Preparing Content

Like the legacy client, all runtime content and configuration live in a `data`
folder next to the built executable, so a default `dotnet run` expects runtime
content under:

```text id="s0ezs7"
src/VNO.Client/bin/Debug/net10.0/data/
```

The runtime content tree is expected to look like this:

```text id="57jkpm"
data/
  settings.ini
  AS.ini
  UI/<design>/design.ini
  characters/<character>/
  background/
  misc/RosterImage/
  misc/BigArt/
  sounds/
```

`settings.ini` and `AS.ini` ship with the app and are copied next to the
executable on build. Edit those copies (or the ones in your own `data` folder) to
point the client at a different game or auth server.

## Running

Run the client:

```bash id="srn46d"
dotnet run --project src/VNO.Client/VNO.Client.csproj
```

For a local full-stack setup:

1. Start `VNO.Master` for account login, version checks and public server discovery.
2. Start a `VNO.Server` instance for live gameplay traffic.
3. Launch `VNO.Client`.

One important detail: the client currently defaults to `GameServerPort = 16789`, while `VNO.Server` currently defaults to `ListenPort = 6541`. For direct local connections, change one side so the ports match.

## Configuration

Following the legacy client, settings are read from external ini files in the
`data` folder by [`ClientSettingsLoader`](src/VNO.Client/Services/ClientSettingsLoader.cs),
not from an `appsettings.json`. Any key that is missing falls back to a built in
default, so a partial or absent file still starts the client.

`data/settings.ini`:

- `[User] user`: default local display name
- `[Connection] server` / `[Connection] port`: direct game server target
- `[Connection] heartbeat`: keepalive interval in seconds

`data/AS.ini`:

- `[AS] 1`: primary Master Server, a bare host or `host:port` (port defaults to 6543)

The data folder name is fixed at `data` next to the executable, matching the
legacy layout. Environment-variable overrides are not wired up.

## Testing

Run the test suite:

```bash id="6mul4e"
dotnet test VNO.Client.slnx
```

The test suite covers asset loading, INI parsing, theme and colour helpers, replay behaviour, moderator flows and the client-to-server message contract.

## Related Repositories

- [`VNO.Core`](https://github.com/Lemonical/VNO.Core): shared protocol, message framing, models and TCP transport
- [`VNO.Master`](https://github.com/Lemonical/VNO.Master): authentication and server-listing service
- [`VNO.Server`](https://github.com/Lemonical/VNO.Server): game server and staff control surface

## Contributing

Contributions are easiest to review when they stay close to the existing split between views, view models and services.

See [CONTRIBUTING.md](CONTRIBUTING.md) for setup expectations and pull request guidance.

Before opening a pull request:

```bash id="kwyl67"
git submodule update --init --recursive
dotnet test VNO.Client.slnx
```

If you change asset loading, replay behaviour, moderator flows or protocol-facing behaviour, include the matching test updates.

## Support

Use the [GitHub issue tracker](https://github.com/Lemonical/VNO.Client/issues) for bugs, regressions, asset-loading problems and client workflow feedback.
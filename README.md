<p align="center">
  <img src="https://raw.githubusercontent.com/Lemonical/VNO.Core/refs/heads/main/docs/assets/vno-icon.png" width="96" alt="Visual Novel Online icon">
</p>

<h1 align="center">Visual Novel Online Client</h1>

<p align="center">The cross-platform desktop player for Visual Novel Online.</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/"><img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white"></a>
  <a href="https://avaloniaui.net/"><img alt="Avalonia 12" src="https://img.shields.io/badge/Avalonia-12.0.4-8B44AC"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/github/license/Lemonical/VNO.Client"></a>
  <a href="https://github.com/Lemonical/VNO.Client/commits/main"><img alt="Last commit" src="https://img.shields.io/github/last-commit/Lemonical/VNO.Client/main"></a>
  <a href="https://github.com/Lemonical/VNO.Client/issues"><img alt="Open issues" src="https://img.shields.io/github/issues/Lemonical/VNO.Client"></a>
</p>

`VNO.Client` is the player-facing application in the Visual Novel Online stack. It uses `VNO.Master` for accounts, version policy, news, public server discovery, badges, and short-lived game handoffs, then connects to `VNO.Server` for live play.

The port deliberately preserves the external legacy content model: themes, characters, backgrounds, sounds, and related assets are loaded from a runtime `data/` directory rather than embedded in the application.

> [!IMPORTANT]
> This project is under active development. Build from source; installers and official binary releases are not currently provided by this repository.

## Features

- Avalonia MVVM desktop UI for login, server browsing, character selection, and the game stage
- Account login and creation, version checks, news, public discovery, and authenticated game handoff
- TCP and WebSocket/WSS connections through the shared `VNO.Core` protocol
- In-character and out-of-character chat, areas, music, rosters, stage effects, HP/MP, and self visibility
- Legacy theme, sprite, emote, background, big-art, sound, and INI loading
- Master-issued speaker badges and moderator-facing gameplay flows
- Replay behavior and BASS-backed audio with graceful no-audio fallback when native BASS is absent
- Privacy-tiered Discord Rich Presence over local desktop IPC, disabled by default
- Automated coverage for assets, settings, protocol-facing services, replay, and moderation behavior

## Quick start

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git with submodule support
- A compatible VNO runtime content tree
- Native BASS libraries only when audio playback is required

Windows desktop is represented in the project configuration. Other Avalonia desktop platforms may build from source, but this repository does not publish or test a RID/package matrix.

### Install and build

```bash
git clone --recurse-submodules https://github.com/Lemonical/VNO.Client.git
cd VNO.Client
dotnet restore VNO.Client.slnx
dotnet build VNO.Client.slnx
```

For an existing clone without its submodule:

```bash
git submodule update --init --recursive
```

### Prepare content

The application resolves `data/` next to its executable. A Debug run therefore uses `src/VNO.Client/bin/Debug/net10.0/data/`. The build copies the default `settings.ini`; supply the remaining compatible content yourself. The public Master endpoint and application version are shared constants in `VNO.Core`, not player-editable files.

```text
data/
  settings.ini
  UI/<design>/design.ini
  characters/<character>/
  background/
  misc/RosterImage/
  misc/BigArt/
  sounds/
```

Do not commit licensed runtime content.

### Run

```bash
dotnet run --project src/VNO.Client/VNO.Client.csproj
```

For a complete local environment, start Master, start a Server, and then launch Client. Server-list entries carry their own game endpoint. Direct connections currently fall back to `127.0.0.1:16789`, while Server defaults to port `6541`, so direct local testing must align those values.

## Configuration summary

`ClientSettingsLoader` reads these player-owned settings:

| File | Setting | Meaning |
| --- | --- | --- |
| `data/settings.ini` | `[User] user` | Default display name |
| `data/settings.ini` | `[Network] transport` | Game transport: `tcp`, `ws`, or `websocket` |
| `data/settings.ini` | `[Network] tls` | Use TLS for the game WebSocket connection |
| `data/settings.ini` | `[Discord] presence` | `Off`, `Running`, `PublicServer`, or `PublicServerAndPlayerCount` |
| `data/settings.ini` | `[Discord] application_id` | Public Discord application ID for local Rich Presence IPC |

Missing values use built-in defaults. Discord presence stays off unless both a public application ID and a non-`Off` privacy level are configured; never place a client secret or bot token in this file. Only public-directory server names and validated player counts are eligible for presence. The loader does not currently read game host, game port, or heartbeat values from INI files. Theme selection also reads `[DesignStyle] design`; the login flow separately reads and writes `[User] enabled`, `user`, and `pass` for Remember Me. The saved `pass` is reusable legacy credential material and should be protected like a password.

The full runtime-data, theme, audio, configuration, and local-stack guides belong in the VNO.Client GitHub wiki once it is enabled.

## Build, test, and publish

```bash
dotnet build VNO.Client.slnx -c Release
dotnet test VNO.Client.slnx
dotnet publish src/VNO.Client/VNO.Client.csproj -c Release -o ./publish/client
```

The last command produces a framework-dependent publish directory, not an installer.

## Repository layout

```text
src/VNO.Client/        Application, views, view models, and services
tests/VNO.Client.Tests Client unit and behavior tests
external/VNO.Core/     Shared protocol submodule
```

## Docker

Client is an interactive desktop application and does not ship a Docker image. Use the Docker deployments in `VNO.Master` and `VNO.Server` for headless infrastructure.

## Ecosystem

- [VNO.Core](https://github.com/Lemonical/VNO.Core) - shared protocol, models and transports
- [VNO.Master](https://github.com/Lemonical/VNO.Master) - accounts, authentication, news and server directory
- [VNO.Server](https://github.com/Lemonical/VNO.Server) - game hosting and staff administration

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a change. Use the [issue tracker](https://github.com/Lemonical/VNO.Client/issues) for reproducible bugs and feature requests, and never include credentials or private content in reports.

## License

VNO.Client is licensed under the [MIT License](LICENSE).

# Contributing to VNO.Client

Contributions should preserve the separation between Avalonia views, view models, and services while remaining compatible with `VNO.Core` and the external legacy content layout.

## Setup

Requirements are the .NET 10 SDK, Git with submodule support, and compatible runtime content when testing asset-driven behavior. Native BASS is optional unless the change affects audio.

```bash
git clone --recurse-submodules https://github.com/Lemonical/VNO.Client.git
cd VNO.Client
dotnet restore VNO.Client.slnx
dotnet build VNO.Client.slnx
dotnet test VNO.Client.slnx
```

Use `git submodule update --init --recursive` if Core was not initialized.

## Change guidelines

- Keep application behavior out of views and code-behind unless it directly depends on visual elements.
- Keep protocol-facing behavior aligned with `external/VNO.Core`.
- Preserve tolerant loading for missing or partial legacy content unless deliberately changing that contract.
- Load player themes and content from `data/`; do not embed or commit licensed runtime assets.
- Preserve separate Master and game-server connection responsibilities.
- Treat authentication, handoff, TLS, moderation, and deserialization changes as security-sensitive.
- Keep changes focused and avoid unrelated formatting or refactoring.
- Update configuration documentation when a loader key or default changes.

## Testing

Run `dotnet test VNO.Client.slnx` for every change. Add focused tests when changing:

- INI parsing, configuration defaults, or endpoint selection
- Themes, colors, characters, rosters, backgrounds, big art, or sound discovery
- Login, account creation, server discovery, or game handoff
- Discord presence privacy projection, local IPC framing, sanitization, or failure isolation
- Message handling, framing assumptions, reconnect behavior, or cancellation
- Stage, replay, visibility, stats, badges, effects, or moderation flows

Manually exercise the relevant desktop workflow when UI or runtime content behavior changes. State which operating system, content set, Master, and Server configuration you used.

## Documentation

Keep the README to a concise overview and quick start. Put full tutorials, theme/content references, configuration matrices, and troubleshooting in the VNO.Client GitHub wiki once it is enabled. Update docs with the behavior change that makes them necessary.

## Pull requests and issues

A pull request should explain the player-visible impact, include tests, pass the build and test suite, identify Core/Master/Server compatibility effects, and contain no secrets or licensed content. Keep unrelated cleanup separate.

Bug reports should include reproducible steps, expected and actual results, OS, .NET SDK version, relevant repository revisions, content availability, connection mode, and sanitized logs. Do not disclose credentials, bearer tokens, private server data, or exploitable security details publicly.

## License

By contributing, you agree that your contribution is licensed under this project's [MIT License](LICENSE).

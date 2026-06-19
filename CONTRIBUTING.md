# Contributing to VNO.Client

`VNO.Client` is the Avalonia desktop player client for the Visual Novel Online stack. Contributions should preserve the current split between views, view models and services, while staying compatible with the shared `VNO.Core` protocol contract and the legacy runtime asset layout.

## Prerequisites

* .NET 10 SDK
* Git with submodule support
* Legacy runtime content, if you need to exercise asset-driven UI behaviour locally
* Optional: native BASS runtime, if you need to test music or sound playback

## Setup

Clone the repository with submodules:

```bash id="fczhqb"
git clone --recurse-submodules https://github.com/Lemonical/VNO.Client.git
cd VNO.Client
dotnet restore VNO.Client.slnx
```

If you already cloned the repository without submodules:

```bash id="cx5v2q"
git submodule update --init --recursive
```

## Development Notes

When making changes, please keep the following in mind:

* Keep protocol-facing behaviour aligned with `external/VNO.Core`.
* Preserve the split between views, view models and services.
* Keep asset loading tolerant of partial or missing legacy content unless the change intentionally tightens that contract.
* Keep runtime content loaded from the `data/` folder rather than embedding legacy assets into the assembly.
* Avoid mixing unrelated refactors with user-visible or protocol-visible behaviour changes.
* Prefer small, focused changes that are easier to review.
* Add comments for non-obvious logic, especially around protocol handling, asset discovery, theme loading, replay behaviour and moderation flows.

## Testing

Run the project test suite before opening a pull request:

```bash id="pz0xg7"
dotnet test VNO.Client.slnx
```

Add or update tests when changing:

* Asset loading
* INI parsing
* Theme and colour helpers
* Character roster loading
* Background, big-art or sound lookup
* Replay behaviour
* Moderator flows
* Client-to-server message handling
* Master Server login or server-list behaviour
* Configuration loading

## Pull Requests

Before opening a pull request, make sure that:

* Submodules are initialised and up to date.
* The project builds successfully.
* The test suite passes.
* Asset-loading or protocol changes are covered by matching tests.
* Any configuration changes are documented.
* The pull request explains the user-visible or protocol-visible impact clearly.

Please include setup notes if the change depends on local runtime content, BASS, a local `VNO.Master` instance or a local `VNO.Server` instance.

Keep unrelated cleanup out of the same pull request unless it is required for the main change.

## Issues

Use GitHub issues to report bugs, regressions, asset-loading problems, client workflow issues or protocol-related concerns.

When reporting a bug, please include:

* What you expected to happen
* What actually happened
* Steps to reproduce the issue
* Relevant logs or screenshots, if available
* Your operating system
* Your .NET SDK version
* Whether legacy runtime content is present
* Whether the issue involves `VNO.Master`, `VNO.Server` or local direct connection settings

## License

By contributing to this repository, you agree that your contributions will be licensed under the MIT License that covers this project.

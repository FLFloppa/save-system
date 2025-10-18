# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2025-10-18
### Added
- Editor inspectors now leverage `FLFloppa.EditorHelpers` cards and expandable lists for unified layouts across save system assets.
- Dedicated inspectors for `AppData`, `Game Folder`, and `Custom` path strategy assets including live path previews.

### Changed
- `SaveServiceConfigurationEditor` and pipeline inspectors present validation, migrator chains, and runtime previews with the new helper widgets for quicker navigation.
- Documentation updated to highlight the manual dependency on `FLFloppa Editor Helpers`.

### Fixed
- Improved error handling while resolving storage paths and migrator previews directly in the inspector UI.

## [0.1.2] - 2025-10-11
### Added
- `PlayerPrefsStorageProvider` and matching ScriptableObject configuration for lightweight persistence.
- Documentation updates describing the PlayerPrefs option alongside file system storage.

### Changed
- Package metadata bumped to `0.1.2`.

## [0.1.1] - 2025-10-11
### Added
- Pluggable storage path strategies (`AppData`, `Game Folder`, `Custom`) with ScriptableObject configurations.
- New editor inspector support for selecting path strategies directly on `FileSystemStorageProvider` assets.
- README and documentation updates covering path strategy usage and GitHub release installation.

### Changed
- `FileSystemStorageProvider` now resolves its root via `IStoragePathStrategy` instead of a fixed directory string.
- Package dependency list updated to reference `com.unity.nuget.newtonsoft-json`.

### Fixed
- Sample assets updated to align with new storage provider configuration.

## [0.1.0] - 2025-10-10
### Added
- Initial public release of the FLFloppa Save System package.
- Modular runtime with serializers, processing modules, storage providers, and migration pipeline.
- Unity editor tooling including configuration inspectors and the Save Observer window.
- Documentation portal under `Documentation~/` with manual, readable data guide, and migration cookbook.
- Basic save/load sample under `Samples~/BasicSetup`.
- Continuous integration guidance and community documentation.

### Changed
- `FileSystemStorageProvider` now resolves its root via `IStoragePathStrategy` instead of a fixed directory string.
- Package dependency list updated to reference `com.unity.nuget.newtonsoft-json`.
- Sample assets updated to align with new storage provider configuration.

# FLFloppa Save System Manual

Welcome to the FLFloppa Save System manual. This guide describes how to install the package, configure the save pipeline inside the Unity Editor, and integrate the runtime API in your game.

## Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuring the save service](#configuring-the-save-service)
- [Working with saves at runtime](#working-with-saves-at-runtime)
- [Editor tooling](#editor-tooling)
- [Extending the package](#extending-the-package)
- [Further reading](#further-reading)

## Overview

The save system separates persistence into four interchangeable layers:

1. **Serialization** – Converts objects into portable payloads using an `ISerializer` implementation.
2. **Processing** – Optionally compresses, encrypts, or otherwise mutates data via `IProcessingModule` chains.
3. **Storage** – Persists payloads using an `IStorageProvider` (local file system by default).
4. **Migration** – Upgrades legacy payloads through `IDataMigrator` steps when the schema version changes.

Each layer is configured through ScriptableObject assets created from the Unity editor menus provided by the package. Runtime systems interact solely with the `ISaveService` abstraction, keeping gameplay code independent from concrete implementations.

## Installation

1. Install the package via Git URL (`https://github.com/FLFloppa/save-system.git`) or copy the folder into your project `Packages/` directory.
2. Open Unity to let assembly definitions compile. The runtime, editor and tests assemblies appear under the FLFloppa namespaces.
3. Ensure the dependency `com.cysharp.unitask` resolves (it is declared as a Git dependency in `package.json`).

## Configuring the save service

1. Create the following assets from the Project window:
   * `FLFloppa/Save System/Serializer/Newtonsoft Json`
   * A storage provider asset: `File System Provider` (with optional path strategy) or `PlayerPrefs Provider`
   * `FLFloppa/Save System/Processing/All Modules Pipeline`
   * `FLFloppa/Save System/Save Service Configuration`
2. Open the `Save Service Configuration` asset and assign the serializer, storage provider, and processing pipeline. When using the PlayerPrefs provider you can set a key prefix to group entries by profile or category.
3. Optional: add `Data Migrator` assets to handle version upgrades.
4. Click **Validate & Build Service** to confirm the configuration compiles without errors.

## Working with saves at runtime

```csharp
public sealed class SaveExample : MonoBehaviour
{
    [SerializeField] private SaveServiceConfiguration configuration;
    private ISaveService _service;

    private void Awake()
    {
        _service = configuration.Build();
        _service.SetProfile("Player");
        _service.SetCategory("Campaign");
    }

    public async UniTask SaveAsync(PlayerState state)
    {
        await _service.SaveAsync("player_state", state);
    }
}
```

* Use `Save` / `Load` for synchronous operations and `SaveAsync` / `LoadAsync` for UniTask-based flows.
* Attach metadata that implements `IMetadata` to capture additional context.
* Implement `ISaveReadable` or mark a method with `[SaveReadableMethod]` to surface human-readable summaries inside the Save Observer.

## Editor tooling

* **Save Service Configuration Inspector** – Validates mandatory fields, previews processing order, and highlights migration gaps.
* **Processing Module Inspectors** – Configure compression/encryption modules with built-in guardrails.
* **Storage Provider Inspectors** – Configure file-system path strategies or PlayerPrefs key prefixes, with validation and quick actions.
* **Save Observer Window** – Inspect saved envelopes grouped by profile/category, review metadata and payload previews, and copy summaries for QA.

See the dedicated [Editor Tooling](readable-data.md#editor-tooling) guidance for more detail.

## Extending the package

* Implement `ISerializer`, `IProcessingModule`, `IStorageProvider`, or `IDataMigrator` for custom behaviours.
* Derive from the corresponding `*Configuration` ScriptableObject to expose editor-friendly configuration.
* Add UI Toolkit inspectors to surface additional settings if required.

Further examples are covered in:

* [Readable payloads & metadata](readable-data.md)
* [Designing data migrations](migrations.md)

## Further reading

* `Documentation~/manual/readable-data.md`
* `Documentation~/manual/migrations.md`
* `Documents/professional_unity_save_system_design.md` for the original design brief.

# Basic Save Service Setup

This sample demonstrates how to configure and use the FLFloppa Save System in a fresh Unity project.

## Contents

* `Scripts/SampleSaveBootstrap.cs` – MonoBehaviour that initialises a save service and exposes simple save/load actions.
* `Scripts/SamplePlayerProgress.cs` – Serializable payload used in the demo.
* `Scenes/SampleSaveScene.unity` – Minimal scene wiring the sample together (optional; create manually if you prefer).

## Using the sample

1. Import the sample from **Window → Package Manager → FLFloppa Save System → Samples → Basic Save Service Setup → Import**.
2. Open the `SampleSaveScene` scene (or create a new scene and add the `SampleSaveBootstrap` component to a GameObject).
3. In the Project window, create the following assets:
   - `FLFloppa/Save System/Serializer/Newtonsoft Json`
   - `FLFloppa/Save System/Storage/File System Provider`
   - `FLFloppa/Save System/Processing/All Modules Pipeline`
   - `FLFloppa/Save System/Save Service Configuration`
4. Assign the serializer, storage provider, and pipeline to the `Save Service Configuration` asset.
5. Drag the configuration asset onto the `SampleSaveBootstrap` component.
6. Enter Play Mode and use the UI buttons to save, load, and inspect the data through the Save Observer window (`FLFloppa → Save System → Save Observer`).

## Notes

The sample stores data under the default file-system root configured by the `FileSystemStorageProvider`. Delete the generated save files from your project's `Saves/` directory to reset the sample.

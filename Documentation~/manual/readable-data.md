# Readable payloads & metadata

Designer-friendly save data is critical when using the Save Observer window. This guide explains how to surface summaries and custom visuals for payloads and metadata.

## Readable summaries

Implement the `ISaveReadable` interface or annotate a parameterless method with `[SaveReadableMethod]` to expose a human-readable string.

```csharp
[Serializable]
public struct PlayerCheckpoint : ISaveReadable
{
    public int levelIndex;
    public TimeSpan playtime;

    public string ToReadableString()
    {
        return $"Level {levelIndex} · {playtime:mm\:ss}";
    }
}
```

### Tips

* Keep summaries short and focused on the information a designer or QA tester needs.
* If you cannot modify the original type, create a wrapper DTO that implements `ISaveReadable` and convert before saving.

## Custom visual elements

Inside editor-only code you can implement `ISaveReadableElement` or add `[SaveReadableElementMethod]` to return a `VisualElement` for the Save Observer UI.

```csharp
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

[Serializable]
public struct CheckpointMetadata : ISaveReadable
#if UNITY_EDITOR
    , ISaveReadableElement
#endif
{
    public string sceneName;
    public Texture2D thumbnail;

    public string ToReadableString() => sceneName;

#if UNITY_EDITOR
    public VisualElement CreateReadableElement()
    {
        var column = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                gap = 4
            }
        };

        if (thumbnail != null)
        {
            column.Add(new Image { image = thumbnail, scaleMode = ScaleMode.ScaleToFit });
        }

        column.Add(new Label($"Scene: {sceneName}"));
        return column;
    }
#endif
}
```

## Metadata best practices

* Build composite metadata by using `CompositeMetadata` when multiple snippets of data need to travel alongside a payload.
* Destroy temporary textures or other heavy allocations in `DetachFromPanelEvent` handlers to avoid memory leaks.
* Use metadata to embed audit information: save timestamps, game version, difficulty, etc.

## Debugging readable output

1. Save a payload or metadata instance that implements the readable interfaces.
2. Open the Save Observer window (`FLFloppa → Save System → Save Observer`).
3. Locate the save entry – summaries appear directly on the card, and custom visuals render beneath the summary text.

## Further reading

* [Manual index](index.md)
* [Migration cookbook](migrations.md)

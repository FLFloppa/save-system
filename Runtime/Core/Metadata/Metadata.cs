using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
#endif

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Marker interface for metadata stored alongside save data.
    /// </summary>
    public interface IMetadata
    {
    }

    /// <summary>
    /// Standard metadata captured for each save.
    /// </summary>
    [Serializable]
    public class StandardMetadata : IMetadata, ISaveReadable
    {
        /// <summary>
        /// Gets or sets the timestamp recorded when the save occurred (UTC recommended).
        /// </summary>
        public DateTime SaveTime { get; set; }

        /// <summary>
        /// Gets or sets the version string of the game/client that produced the save.
        /// </summary>
        public string GameVersion { get; set; }

        /// <summary>
        /// Produces a concise textual representation of the metadata contents.
        /// </summary>
        /// <returns>A human-readable summary.</returns>
        public virtual string ToReadableString()
        {
            return $"Saved {SaveTime:G} (Game {GameVersion ?? "unknown"})";
        }
    }

    /// <summary>
    /// Extended metadata that includes a screenshot payload.
    /// </summary>
    [Serializable]
    public class ScreenshotMetadata : StandardMetadata
#if UNITY_EDITOR
        , ISaveReadableElement
#endif
    {
        /// <summary>
        /// PNG-encoded bytes of a screenshot captured at save time.
        /// </summary>
        public byte[] ScreenshotData { get; set; }

#if UNITY_EDITOR
        /// <inheritdoc />
        public override string ToReadableString()
        {
            return base.ToReadableString() + (ScreenshotData is { Length: > 0 } ? " • Includes screenshot" : string.Empty);
        }

        /// <inheritdoc />
        public VisualElement CreateReadableElement()
        {
            if (ScreenshotData is not { Length: > 0 })
            {
                return null;
            }

            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 4
                }
            };

            var info = new Label("Screenshot Preview")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 2
                }
            };
            container.Add(info);

            var texture = LoadTexture(ScreenshotData);
            if (texture == null)
            {
                container.Add(new Label("Unable to decode screenshot bytes."));
                return container;
            }

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                image = texture
            };
            image.style.maxHeight = 180;
            image.style.maxWidth = 320;
            image.RegisterCallback<DetachFromPanelEvent>(_ => UnityEngine.Object.DestroyImmediate(texture));
            container.Add(image);

            return container;
        }

        /// <summary>
        /// Attempts to decode a texture from the provided PNG bytes.
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        /// <returns>A temporary texture instance or <c>null</c> if decoding fails.</returns>
        private static Texture2D LoadTexture(byte[] data)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Save Screenshot"
            };

            if (!texture.LoadImage(data))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            return texture;
        }
#endif
    }

    /// <summary>
    /// Metadata container that aggregates multiple metadata objects.
    /// </summary>
    [Serializable]
    public class CompositeMetadata : IMetadata, ISaveReadable
#if UNITY_EDITOR
        , ISaveReadableElement
#endif
    {
        private readonly List<IMetadata> _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeMetadata"/> class with no child metadata entries.
        /// </summary>
        public CompositeMetadata()
        {
            _items = new List<IMetadata>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeMetadata"/> class with the provided metadata entries.
        /// </summary>
        /// <param name="items">The initial metadata collection to copy into this composite.</param>
        public CompositeMetadata(IEnumerable<IMetadata> items)
        {
            _items = items != null
                ? new List<IMetadata>(Sanitize(items))
                : new List<IMetadata>();
        }

        /// <summary>
        /// Gets the metadata entries contained in this composite.
        /// </summary>
        public IReadOnlyList<IMetadata> Items => _items;

        /// <summary>
        /// Adds the specified metadata entry to the composite if it is not null.
        /// </summary>
        /// <param name="metadata">The metadata entry to add.</param>
        public void Add(IMetadata metadata)
        {
            if (metadata != null)
            {
                _items.Add(metadata);
            }
        }

        /// <summary>
        /// Removes the specified metadata entry from the composite.
        /// </summary>
        /// <param name="metadata">The metadata entry to remove.</param>
        /// <returns><c>true</c> if the entry was removed; otherwise, <c>false</c>.</returns>
        public bool Remove(IMetadata metadata)
        {
            return metadata != null && _items.Remove(metadata);
        }

        /// <summary>
        /// Removes all metadata entries from the composite.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }

        /// <inheritdoc />
        public string ToReadableString()
        {
            if (_items.Count == 0)
            {
                return "(no metadata)";
            }

            var lines = new List<string>(_items.Count);
            foreach (var metadata in _items)
            {
                if (metadata == null)
                {
                    continue;
                }

                if (ReadableUtility.TryGetReadableString(metadata, out var readable) && !string.IsNullOrWhiteSpace(readable))
                {
                    lines.Add(readable);
                }
                else
                {
                    lines.Add(metadata.ToString());
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        public VisualElement CreateReadableElement()
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    backgroundColor = new Color(0f, 0f, 0f, 0.08f),
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3
                }
            };

            if (_items.Count == 0)
            {
                container.Add(new Label("(no metadata)")
                {
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Italic,
                        color = new Color(0.8f, 0.8f, 0.8f, 0.85f)
                    }
                });
                return container;
            }

            for (var index = 0; index < _items.Count; index++)
            {
                var metadata = _items[index];
                if (metadata == null)
                {
                    continue;
                }

                VisualElement element = null;
                if (ReadableUtility.TryGetReadableElement(metadata, out var customElement) && customElement != null)
                {
                    element = customElement;
                }
                else if (ReadableUtility.TryGetReadableString(metadata, out var readable) && !string.IsNullOrWhiteSpace(readable))
                {
                    element = new Label(readable)
                    {
                        style =
                        {
                            whiteSpace = WhiteSpace.Normal
                        }
                    };
                }
                else
                {
                    element = new Label(metadata.ToString())
                    {
                        style =
                        {
                            whiteSpace = WhiteSpace.Normal
                        }
                    };
                }

                if (element != null)
                {
                    element.style.marginBottom = 4;
                    container.Add(element);
                }

                if (index < _items.Count - 1)
                {
                    container.Add(CreateSeparator());
                }
            }

            return container;
        }

        /// <summary>
        /// Filters null entries from the provided metadata sequence.
        /// </summary>
        /// <param name="items">The metadata entries to sanitize.</param>
        /// <returns>A sequence containing only non-null metadata entries.</returns>
        private static IEnumerable<IMetadata> Sanitize(IEnumerable<IMetadata> items)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Creates a simple separator element used between metadata entries in the inspector.
        /// </summary>
        /// <returns>A configured separator visual element.</returns>
        private static VisualElement CreateSeparator()
        {
            return new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(1f, 1f, 1f, 0.1f),
                    marginBottom = 4
                }
            };
        }
#endif
    }
}

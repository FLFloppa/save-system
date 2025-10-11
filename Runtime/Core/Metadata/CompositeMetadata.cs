using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem
{
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
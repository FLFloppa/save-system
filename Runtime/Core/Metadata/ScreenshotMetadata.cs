using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem
{
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
}
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(GzipProcessingModuleAsset))]
    public sealed class GzipProcessingModuleAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    flexDirection = FlexDirection.Column
                }
            };

            var header = new Label("GZip Compression Module");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            root.Add(header);

            root.Add(new Label("Compresses save payloads with GZip during save and decompresses during load. Recommended for large payloads."));

            var info = new HelpBox(
                "Note: Compression increases CPU usage. Place this module before encryption in the pipeline for best results.",
                HelpBoxMessageType.Info);
            info.style.marginTop = 4;
            root.Add(info);

            return root;
        }
    }
}

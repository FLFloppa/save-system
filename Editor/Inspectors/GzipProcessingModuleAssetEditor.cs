using FLFloppa.EditorHelpers;
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
            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "GZip Compression Module",
                "Compresses payloads during save and decompresses on load. Ideal for large save data."));

            var overviewCard = InspectorUi.Cards.Create("Usage Guidance", out var overviewContent);
            InspectorUi.Controls.AddHelpBox(
                overviewContent,
                "Compression increases CPU usage. Place this module before encryption to maximize effectiveness.",
                HelpBoxMessageType.Info);

            root.Add(overviewCard);

            return root;
        }
    }
}

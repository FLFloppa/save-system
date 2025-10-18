using System;
using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(CustomPathStrategyAsset))]
    public sealed class CustomPathStrategyAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "Custom Path Strategy",
                "Resolves save data using a user-specified path relative to the project or absolute."));

            var configurationCard = InspectorUi.Cards.Create("Configuration", out var configurationContent);
            var pathProperty = so.FindProperty("_path");
            configurationContent.Add(InspectorUi.Controls.CreatePropertyField(pathProperty, "Path"));
            root.Add(configurationCard);

            var previewCard = InspectorUi.Cards.Create("Resolved Path", out var previewContent);
            var previewLabel = InspectorUi.Controls.CreateSummaryLabel(string.Empty);
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            previewContent.Add(previewLabel);
            root.Add(previewCard);

            void RefreshPreview()
            {
                try
                {
                    var strategy = ((CustomPathStrategyAsset)target).Build();
                    previewLabel.text = $"Resolved Path: {strategy.GetPath()}";
                }
                catch (Exception ex)
                {
                    previewLabel.text = $"Unable to resolve path: {ex.Message}";
                }
            }

            root.RegisterCallback<GeometryChangedEvent>(_ => RefreshPreview());
            root.schedule.Execute(RefreshPreview).Every(500);

            return root;
        }
    }
}

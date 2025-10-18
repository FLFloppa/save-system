using System;
using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(GameFolderPathStrategyAsset))]
    public sealed class GameFolderPathStrategyAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "Game Folder Path Strategy",
                "Resolves save data relative to the game folder (parent of `Application.dataPath`)."));

            var configurationCard = InspectorUi.Cards.Create("Configuration", out var configurationContent);
            var subdirectoryProperty = so.FindProperty("_subdirectory");
            configurationContent.Add(InspectorUi.Controls.CreatePropertyField(subdirectoryProperty, "Subdirectory"));
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
                    var strategy = ((GameFolderPathStrategyAsset)target).Build();
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

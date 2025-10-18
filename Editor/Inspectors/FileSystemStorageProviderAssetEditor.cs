using System.IO;
using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(FileSystemStorageProviderAsset))]
    public sealed class FileSystemStorageProviderAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "File System Storage Provider",
                "Builds a storage provider that writes save files to disk using a strategy-defined root."));

            var pathCard = InspectorUi.Cards.Create("Configuration", out var pathContent);
            var pathProperty = so.FindProperty("_pathStrategy");
            pathContent.Add(CreatePropertyField(pathProperty, "Path Strategy"));

            var resolvedCard = InspectorUi.Cards.Create("Resolved Path", out var resolvedContent);
            var pathLabel = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            resolvedContent.Add(pathLabel);

            var actionsRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginTop = InspectorUi.Layout.ButtonSpacing
                }
            };
            actionsRow.Add(InspectorUi.Controls.CreateActionButton("Open Folder", RevealInExplorer));
            resolvedContent.Add(actionsRow);

            root.Add(pathCard);
            root.Add(resolvedCard);

            root.RegisterCallback<GeometryChangedEvent>(_ => UpdateResolvedPath(pathLabel));
            root.schedule.Execute(() => UpdateResolvedPath(pathLabel)).Every(500);

            return root;
        }

        private void UpdateResolvedPath(Label label)
        {
            try
            {
                var asset = (FileSystemStorageProviderAsset)target;
                var provider = asset.Build() as FileSystemStorageProvider;
                if (provider == null)
                {
                    label.text = "Unable to resolve storage provider.";
                    return;
                }

                label.text = $"Resolved Path: {provider.RootPath}";
            }
            catch (System.Exception ex)
            {
                label.text = $"Failed to resolve path: {ex.Message}";
            }
        }

        private void RevealInExplorer()
        {
            try
            {
                var asset = (FileSystemStorageProviderAsset)target;
                var provider = asset.Build() as FileSystemStorageProvider;
                if (provider == null)
                {
                    return;
                }

                var path = provider.RootPath;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                EditorUtility.RevealInFinder(path);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Open Folder Failed", ex.Message, "Close");
            }
        }

        private VisualElement CreatePropertyField(SerializedProperty property, string label)
        {
            return property != null
                ? InspectorUi.Controls.CreatePropertyField(property, label)
                : new PropertyField { label = label };
        }
    }
}

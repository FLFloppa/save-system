using System.IO;
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

            var header = new Label("File System Storage Provider");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            root.Add(header);

            root.Add(CreatePropertyField("_pathStrategy", "Path Strategy"));

            var pathLabel = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            root.Add(pathLabel);

            root.RegisterCallback<GeometryChangedEvent>(_ => UpdateResolvedPath(pathLabel));
            root.schedule.Execute(() => UpdateResolvedPath(pathLabel)).Every(500);

            var openButton = new Button(() => RevealInExplorer()) { text = "Open Folder" };
            openButton.style.marginTop = 4;
            root.Add(openButton);

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

        private PropertyField CreatePropertyField(string propertyPath, string label)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                return new PropertyField { label = label };
            }

            var field = new PropertyField(property, label);
            field.Bind(serializedObject);
            return field;
        }
    }
}

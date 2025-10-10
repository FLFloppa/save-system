using System;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(AllProcessingPipelineAsset))]
    public sealed class AllProcessingPipelineAssetEditor : UnityEditor.Editor
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

            var header = new Label("All Modules Processing Pipeline");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            root.Add(header);

            root.Add(new Label("Modules are executed in order during save and reversed during load."));

            var modulesField = CreatePropertyField("_modules", "Processing Modules");
            root.Add(modulesField);

            var previewBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            previewBox.style.marginTop = 4;
            root.Add(previewBox);

            void RefreshPreview()
            {
                try
                {
                    var pipelineAsset = (AllProcessingPipelineAsset)target;
                    var pipeline = pipelineAsset.Build();
                    var chain = pipeline.GetProcessingChain();

                    if (chain == null || chain.Count == 0)
                    {
                        previewBox.messageType = HelpBoxMessageType.Info;
                        previewBox.text = "No modules configured – data will be stored without additional processing.";
                        return;
                    }

                    var builder = new StringBuilder();
                    builder.AppendLine("Module Execution Order:");
                    for (var i = 0; i < chain.Count; i++)
                    {
                        builder.AppendLine($"{i + 1}. {chain[i].GetType().Name}");
                    }

                    previewBox.messageType = HelpBoxMessageType.Info;
                    previewBox.text = builder.ToString();
                }
                catch (Exception ex)
                {
                    previewBox.messageType = HelpBoxMessageType.Warning;
                    previewBox.text = $"Unable to build pipeline: {ex.Message}";
                }
            }

            root.RegisterCallback<GeometryChangedEvent>(_ => RefreshPreview());
            root.schedule.Execute(RefreshPreview).Every(500);

            return root;
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

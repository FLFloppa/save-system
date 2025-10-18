using System;
using System.Text;
using FLFloppa.EditorHelpers;
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

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "All Modules Processing Pipeline",
                "Modules execute sequentially on save and in reverse on load."));

            var modulesCard = InspectorUi.Cards.Create("Modules", out var modulesContent);
            var modulesProperty = so.FindProperty("_modules");
            modulesContent.Add(CreatePropertyField(modulesProperty, "Processing Modules"));

            var previewCard = InspectorUi.Cards.Create(
                "Runtime Preview",
                out var previewContent,
                "Preview the runtime processing order produced by this configuration.");
            var moduleList = InspectorUi.ExpandableLists.Create(
                "Module Order",
                "Modules execute sequentially when saving and in reverse when loading.",
                expanded: true);
            previewContent.Add(moduleList.Root);
            var previewBox = InspectorUi.Controls.AddHelpBox(previewContent, string.Empty, HelpBoxMessageType.Info);

            root.Add(modulesCard);
            root.Add(previewCard);

            void RefreshPreview()
            {
                moduleList.ClearItems();

                try
                {
                    var pipelineAsset = (AllProcessingPipelineAsset)target;
                    var pipeline = pipelineAsset.Build();
                    var chain = pipeline.GetProcessingChain();

                    if (chain == null || chain.Count == 0)
                    {
                        previewBox.messageType = HelpBoxMessageType.Info;
                        previewBox.text = "No modules configured – data will be stored without additional processing.";
                        moduleList.ShowEmptyState("Pipeline currently empty.");
                        return;
                    }

                    previewBox.messageType = HelpBoxMessageType.Info;
                    previewBox.text = $"Configured {chain.Count} module(s).";

                    for (var i = 0; i < chain.Count; i++)
                    {
                        moduleList.AddItem($"{i + 1}. {chain[i].GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    previewBox.messageType = HelpBoxMessageType.Warning;
                    previewBox.text = $"Unable to build pipeline: {ex.Message}";
                    moduleList.ShowEmptyState("Preview unavailable.");
                }
            }

            root.RegisterCallback<GeometryChangedEvent>(_ => RefreshPreview());
            root.schedule.Execute(RefreshPreview).Every(500);

            return root;
        }

        private VisualElement CreatePropertyField(SerializedProperty property, string label)
        {
            return property != null
                ? InspectorUi.Controls.CreatePropertyField(property, label)
                : new PropertyField { label = label };
        }
    }
}

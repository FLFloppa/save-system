using System;
using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(SamplePlayerProfileMigratorAsset))]
    public sealed class SamplePlayerProfileMigratorAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "Sample Player Profile Migrator",
                "Demonstrates how to upgrade player profile data between versions."));

            var configurationCard = InspectorUi.Cards.Create("Configuration", out var configurationContent);
            configurationContent.Add(CreatePropertyField(so.FindProperty("_fromVersion"), "From Version"));
            configurationContent.Add(CreatePropertyField(so.FindProperty("_toVersion"), "To Version"));
            configurationContent.Add(CreatePropertyField(so.FindProperty("_defaultItemId"), "Default Item Id"));

            var validationCard = InspectorUi.Cards.Create("Validation", out var validationContent);
            var validation = InspectorUi.Controls.AddHelpBox(validationContent, string.Empty, HelpBoxMessageType.Info);

            root.Add(configurationCard);
            root.Add(validationCard);

            root.RegisterCallback<GeometryChangedEvent>(_ => RefreshValidation(validation));
            root.schedule.Execute(() => RefreshValidation(validation)).Every(500);

            return root;
        }

        private void RefreshValidation(HelpBox box)
        {
            try
            {
                var asset = (SamplePlayerProfileMigratorAsset)target;
                var migrator = asset.Build();

                if (migrator.FromVersion >= migrator.ToVersion)
                {
                    box.messageType = HelpBoxMessageType.Error;
                    box.text = "FromVersion must be less than ToVersion.";
                    return;
                }

                box.messageType = HelpBoxMessageType.Info;
                box.text = "Migration configuration valid.";
            }
            catch (Exception ex)
            {
                box.messageType = HelpBoxMessageType.Error;
                box.text = ex.Message;
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

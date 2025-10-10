using System;
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

            var header = new Label("Sample Player Profile Migrator");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            root.Add(header);

            root.Add(new Label("Demonstrates how to upgrade player profile data between versions."));

            root.Add(CreatePropertyField("_fromVersion", "From Version"));
            root.Add(CreatePropertyField("_toVersion", "To Version"));
            root.Add(CreatePropertyField("_defaultItemId", "Default Item Id"));

            var validation = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            validation.style.marginTop = 4;
            root.Add(validation);

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

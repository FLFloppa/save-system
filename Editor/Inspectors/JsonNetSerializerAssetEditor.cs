using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(JsonNetSerializerAsset))]
    public sealed class JsonNetSerializerAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "Newtonsoft.Json Serializer",
                "Configure Json.NET behaviors. Type name handling defaults to Auto for polymorphic metadata."));

            var settingsCard = InspectorUi.Cards.Create("Serialization Options", out var settingsContent);
            settingsContent.Add(CreatePropertyField(so.FindProperty("_typeNameHandling"), "Type Name Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_nullValueHandling"), "Null Value Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_missingMemberHandling"), "Missing Member Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_defaultValueHandling"), "Default Value Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_formatting"), "Formatting"));

            root.Add(settingsCard);

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

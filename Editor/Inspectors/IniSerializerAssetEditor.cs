using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(IniSerializerAsset))]
    public sealed class IniSerializerAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "INI Serializer",
                "Serializes objects into human-readable INI sections with strongly typed values."));

            var settingsCard = InspectorUi.Cards.Create("Serialization Options", out var settingsContent);
            settingsContent.Add(CreatePropertyField(so.FindProperty("_typeNameHandling"), "Type Name Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_nullValueHandling"), "Null Value Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_defaultValueHandling"), "Default Value Handling"));
            settingsContent.Add(CreatePropertyField(so.FindProperty("_missingMemberHandling"), "Missing Member Handling"));

            var guidanceCard = InspectorUi.Cards.Create("Guidance", out var guidanceContent);
            InspectorUi.Controls.AddHelpBox(
                guidanceContent,
                "INI serialization suits debugging and human-editable saves. For binary compatibility or complex nested data, consider `JsonNetSerializer`.",
                HelpBoxMessageType.Info);

            root.Add(settingsCard);
            root.Add(guidanceCard);

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

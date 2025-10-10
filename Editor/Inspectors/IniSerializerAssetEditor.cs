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

            var header = new Label("INI Serializer")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginBottom = 4
                }
            };
            root.Add(header);

            root.Add(new Label("Serializes objects into human-readable INI sections with strongly typed values."));

            root.Add(CreatePropertyField("_typeNameHandling", "Type Name Handling"));
            root.Add(CreatePropertyField("_nullValueHandling", "Null Value Handling"));
            root.Add(CreatePropertyField("_defaultValueHandling", "Default Value Handling"));
            root.Add(CreatePropertyField("_missingMemberHandling", "Missing Member Handling"));

            var infoBox = new HelpBox(
                "INI serialization is best for debugging and human-editable saves. For binary compatibility or nested complex data, consider JsonNetSerializer.",
                HelpBoxMessageType.Info)
            {
                style = { marginTop = 4 }
            };
            root.Add(infoBox);

            return root;
        }

        private PropertyField CreatePropertyField(string path, string label)
        {
            var property = serializedObject.FindProperty(path);
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

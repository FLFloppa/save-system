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

            var header = new Label("Newtonsoft.Json Serializer");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            root.Add(header);

            root.Add(new Label("Configure Json.NET behaviors. Type name handling defaults to Auto to enable polymorphic metadata in save envelopes."));

            root.Add(CreatePropertyField("_typeNameHandling", "Type Name Handling"));
            root.Add(CreatePropertyField("_nullValueHandling", "Null Value Handling"));
            root.Add(CreatePropertyField("_missingMemberHandling", "Missing Member Handling"));
            root.Add(CreatePropertyField("_defaultValueHandling", "Default Value Handling"));
            root.Add(CreatePropertyField("_formatting", "Formatting"));

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

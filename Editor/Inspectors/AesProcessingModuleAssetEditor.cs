using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    [CustomEditor(typeof(AesProcessingModuleAsset))]
    public sealed class AesProcessingModuleAssetEditor : UnityEditor.Editor
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

            var header = new Label("AES Encryption Module");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 13;
            header.style.marginBottom = 4;
            root.Add(header);

            root.Add(new Label("Encrypts save payloads with AES/CBC. Keys must be 128/192/256-bit and IV must be 128-bit."));

            var keyDataProp = so.FindProperty("_keyData");
            var keyEncodingProp = so.FindProperty("_keyEncoding");
            var ivDataProp = so.FindProperty("_ivData");
            var ivEncodingProp = so.FindProperty("_ivEncoding");

            root.Add(CreatePropertyField(keyDataProp, "Key Data"));
            root.Add(CreatePropertyField(keyEncodingProp, "Key Encoding"));
            root.Add(CreatePropertyField(ivDataProp, "IV Data"));
            root.Add(CreatePropertyField(ivEncodingProp, "IV Encoding"));

            var validation = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            validation.style.marginTop = 4;
            root.Add(validation);

            var buttonRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 4
                }
            };

            var generateKeyButton = new Button(() =>
            {
                AssignRandomBytes(keyDataProp, keyEncodingProp, 32, validation);
            })
            {
                text = "Generate 256-bit Key"
            };
            generateKeyButton.style.marginRight = 4;

            var generateIvButton = new Button(() =>
            {
                AssignRandomBytes(ivDataProp, ivEncodingProp, 16, validation);
            })
            {
                text = "Generate IV"
            };

            buttonRow.Add(generateKeyButton);
            buttonRow.Add(generateIvButton);
            root.Add(buttonRow);

            root.RegisterCallback<GeometryChangedEvent>(_ => UpdateValidation(validation));
            root.schedule.Execute(() => UpdateValidation(validation)).Every(500);

            return root;
        }

        private void UpdateValidation(HelpBox box)
        {
            try
            {
                var asset = (AesProcessingModuleAsset)target;
                var module = asset.Build() as AesProcessingModule;
                if (module == null)
                {
                    box.messageType = HelpBoxMessageType.Error;
                    box.text = "Failed to build AES module. Check key and IV data.";
                    return;
                }

                box.messageType = HelpBoxMessageType.Info;
                box.text = "AES configuration valid.";
            }
            catch (Exception ex)
            {
                box.messageType = HelpBoxMessageType.Error;
                box.text = ex.Message;
            }
        }

        private void AssignRandomBytes(SerializedProperty dataProp, SerializedProperty encodingProp, int byteCount, HelpBox validation)
        {
            if (dataProp == null || encodingProp == null)
            {
                return;
            }

            var encoded = EncodeBytes(byteCount, encodingProp.enumValueIndex);
            dataProp.stringValue = encoded;
            dataProp.serializedObject.ApplyModifiedProperties();
            UpdateValidation(validation);
        }

        private static string EncodeBytes(int byteCount, int encodingIndex)
        {
            return encodingIndex switch
            {
                0 => BytesToHex(RandomBytes(byteCount)),
                1 => Convert.ToBase64String(RandomBytes(byteCount)),
                2 => GenerateUtf8String(byteCount),
                _ => Convert.ToBase64String(RandomBytes(byteCount))
            };
        }

        private static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        private static string BytesToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
            {
                builder.Append(value.ToString("X2"));
            }

            return builder.ToString();
        }

        private static string GenerateUtf8String(int length)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var bytes = RandomBytes(length);
            var chars = new char[length];

            for (var i = 0; i < length; i++)
            {
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            }

            return new string(chars);
        }

        private PropertyField CreatePropertyField(SerializedProperty property, string label)
        {
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

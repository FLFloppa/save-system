using System;
using System.Security.Cryptography;
using System.Text;
using FLFloppa.EditorHelpers;
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

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "AES Encryption Module",
                "Encrypts payloads with AES/CBC. Keys must be 128/192/256-bit and IV must be 128-bit."));

            var keyDataProp = so.FindProperty("_keyData");
            var keyEncodingProp = so.FindProperty("_keyEncoding");
            var ivDataProp = so.FindProperty("_ivData");
            var ivEncodingProp = so.FindProperty("_ivEncoding");

            var configurationCard = InspectorUi.Cards.Create("Configuration", out var configurationContent);
            configurationContent.Add(InspectorUi.Controls.CreatePropertyField(keyDataProp, "Key Data"));
            configurationContent.Add(InspectorUi.Controls.CreatePropertyField(keyEncodingProp, "Key Encoding"));
            configurationContent.Add(InspectorUi.Controls.CreatePropertyField(ivDataProp, "IV Data"));
            configurationContent.Add(InspectorUi.Controls.CreatePropertyField(ivEncodingProp, "IV Encoding"));

            HelpBox validationBox = null;

            var actionsRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginTop = InspectorUi.Layout.ButtonSpacing
                }
            };

            actionsRow.Add(InspectorUi.Controls.CreateActionButton("Generate 256-bit Key", () =>
            {
                AssignRandomBytes(keyDataProp, keyEncodingProp, 32, validationBox);
            }));

            actionsRow.Add(InspectorUi.Controls.CreateActionButton("Generate IV", () =>
            {
                AssignRandomBytes(ivDataProp, ivEncodingProp, 16, validationBox);
            }));

            configurationContent.Add(actionsRow);

            var validationCard = InspectorUi.Cards.Create("Validation", out var validationContent);
            validationBox = InspectorUi.Controls.AddHelpBox(validationContent, string.Empty, HelpBoxMessageType.Info);

            root.Add(configurationCard);
            root.Add(validationCard);

            root.RegisterCallback<GeometryChangedEvent>(_ => UpdateValidation(validationBox));
            root.schedule.Execute(() => UpdateValidation(validationBox)).Every(500);

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

    }
}

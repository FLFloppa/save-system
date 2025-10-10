using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "AesProcessingModule",
        menuName = "FLFloppa/Save System/Processing/AES Encryption",
        order = 320)]
    public sealed class AesProcessingModuleAsset : ProcessingModuleConfiguration
    {
        private enum KeyEncoding
        {
            Hex,
            Base64,
            Utf8
        }

        [Header("Key Configuration")]
        [SerializeField]
        [Tooltip("Raw key data in the selected encoding (16/24/32 bytes).")]
        private string _keyData;

        [SerializeField]
        [Tooltip("Determines how the key data string is interpreted.")]
        private KeyEncoding _keyEncoding = KeyEncoding.Base64;

        [Header("Initialization Vector")]
        [SerializeField]
        [Tooltip("Raw IV data (16 bytes) in the selected encoding.")]
        private string _ivData;

        [SerializeField]
        [Tooltip("Determines how the IV data string is interpreted.")]
        private KeyEncoding _ivEncoding = KeyEncoding.Base64;

        public override IProcessingModule Build()
        {
            var keyBytes = DecodeBytes(_keyData, _keyEncoding, "key");
            var ivBytes = DecodeBytes(_ivData, _ivEncoding, "initialization vector");
            return new AesProcessingModule(keyBytes, ivBytes);
        }

        private static byte[] DecodeBytes(string data, KeyEncoding encoding, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new SaveSystemException($"AES {fieldName} data cannot be empty.");
            }

            try
            {
                return encoding switch
                {
                    KeyEncoding.Hex => DecodeHex(data.Trim()),
                    KeyEncoding.Base64 => Convert.FromBase64String(data.Trim()),
                    KeyEncoding.Utf8 => Encoding.UTF8.GetBytes(data),
                    _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
                };
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to decode AES {fieldName} data.", ex);
            }
        }

        private static byte[] DecodeHex(string hex)
        {
            var sanitized = hex.Replace(" ", string.Empty);
            if (sanitized.Length % 2 != 0)
            {
                throw new SaveSystemException("Hex data must contain an even number of characters.");
            }

            var bytes = new byte[sanitized.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteValue = sanitized.Substring(i * 2, 2);
                bytes[i] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return bytes;
        }
    }
}

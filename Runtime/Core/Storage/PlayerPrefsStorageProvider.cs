using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Storage provider implementation that persists save payloads inside Unity's PlayerPrefs.
    /// </summary>
    public sealed class PlayerPrefsStorageProvider : IStorageProvider
    {
        private readonly string _keyPrefix;

        public PlayerPrefsStorageProvider(string keyPrefix)
        {
            _keyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? string.Empty : keyPrefix.Trim();
        }

        /// <inheritdoc />
        public void Write(string key, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var prefsKey = BuildKey(key);
            var encoded = Convert.ToBase64String(data);
            PlayerPrefs.SetString(prefsKey, encoded);
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public byte[] Read(string key)
        {
            var prefsKey = BuildKey(key);
            if (!PlayerPrefs.HasKey(prefsKey))
            {
                throw new SaveSystemException($"PlayerPrefs entry '{prefsKey}' does not exist.");
            }

            try
            {
                var encoded = PlayerPrefs.GetString(prefsKey);
                return Convert.FromBase64String(encoded);
            }
            catch (FormatException ex)
            {
                throw new SaveSystemException($"PlayerPrefs entry '{prefsKey}' contains invalid data.", ex);
            }
        }

        /// <inheritdoc />
        public bool Exists(string key)
        {
            var prefsKey = BuildKey(key);
            return PlayerPrefs.HasKey(prefsKey);
        }

        /// <inheritdoc />
        public UniTask WriteAsync(string key, byte[] data)
        {
            Write(key, data);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask<byte[]> ReadAsync(string key)
        {
            var bytes = Read(key);
            return UniTask.FromResult(bytes);
        }

        /// <inheritdoc />
        public UniTask<bool> ExistsAsync(string key)
        {
            var exists = Exists(key);
            return UniTask.FromResult(exists);
        }

        private string BuildKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SaveSystemException("Storage key cannot be null or whitespace.");
            }

            var normalizedKey = key.Replace('/', '_').Replace('\\', '_');
            return string.Concat(_keyPrefix, normalizedKey);
        }
    }
}

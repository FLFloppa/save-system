using System;
using System.IO;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Uses <see cref="Application.persistentDataPath"/> as the storage root.
    /// </summary>
    public sealed class AppDataPathStrategy : IStoragePathStrategy
    {
        private readonly string _subdirectory;

        public AppDataPathStrategy(string subdirectory = null)
        {
            _subdirectory = string.IsNullOrWhiteSpace(subdirectory) ? null : subdirectory.Trim();
        }

        public string GetPath()
        {
            var root = Application.persistentDataPath;
            if (string.IsNullOrEmpty(root))
            {
                throw new InvalidOperationException("Application.persistentDataPath is not available.");
            }

            if (string.IsNullOrEmpty(_subdirectory))
            {
                return Path.GetFullPath(root);
            }

            return Path.GetFullPath(Path.Combine(root, _subdirectory));
        }
    }
}
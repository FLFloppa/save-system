using System;
using System.IO;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Uses the game folder (parent of <see cref="Application.dataPath"/>) as the storage root.
    /// </summary>
    public sealed class GameFolderPathStrategy : IStoragePathStrategy
    {
        private readonly string _subdirectory;

        public GameFolderPathStrategy(string subdirectory = null)
        {
            _subdirectory = string.IsNullOrWhiteSpace(subdirectory) ? null : subdirectory.Trim();
        }

        public string GetPath()
        {
            var dataPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                throw new InvalidOperationException("Application.dataPath is not available.");
            }

            var root = Path.GetFullPath(Path.Combine(dataPath, ".."));
            if (string.IsNullOrEmpty(_subdirectory))
            {
                return root;
            }

            return Path.GetFullPath(Path.Combine(root, _subdirectory));
        }
    }
}
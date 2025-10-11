using System;
using System.IO;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Uses a user-specified path as the storage root.
    /// </summary>
    public sealed class CustomPathStrategy : IStoragePathStrategy
    {
        private readonly string _path;

        public CustomPathStrategy(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
            }

            _path = path;
        }

        public string GetPath()
        {
            return Path.GetFullPath(_path);
        }
    }
}
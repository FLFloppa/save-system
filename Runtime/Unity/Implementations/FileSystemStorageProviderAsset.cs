using System.IO;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "FileSystemStorageProvider",
        menuName = "FLFloppa/Save System/Storage/File System Provider",
        order = 100)]
    public sealed class FileSystemStorageProviderAsset : StorageProviderConfiguration
    {
        [SerializeField]
        [Tooltip("The root directory where save data will be stored.")]
        private string _rootDirectory = "Saves";

        public string RootDirectory => _rootDirectory;

        public override IStorageProvider Build()
        {
            var rootPath = ResolveRootPath(_rootDirectory);
            return new FileSystemStorageProvider(rootPath);
        }

        private static string ResolveRootPath(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new SaveSystemException("Root directory cannot be null or whitespace.");
            }

            if (Path.IsPathRooted(directory))
            {
                return directory;
            }

            // Default to persistent data path in builds and project root in editor for convenience.
#if UNITY_EDITOR
            var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectPath, directory);
#else
            return Path.Combine(Application.persistentDataPath, directory);
#endif
        }
    }
}

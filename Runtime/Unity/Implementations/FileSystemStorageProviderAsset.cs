using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "FileSystemStorageProvider",
        menuName = "FLFloppa/Save System/Storage/File System Provider",
        order = 100)]
    public sealed class FileSystemStorageProviderAsset : StorageProviderConfiguration
    {
        private const string DefaultSubdirectory = "Saves";

        [SerializeField]
        [Tooltip("Strategy asset that resolves the root directory for save data. If omitted, Application.persistentDataPath/Saves is used.")]
        private StoragePathStrategyConfiguration _pathStrategy;

        public StoragePathStrategyConfiguration PathStrategy => _pathStrategy;

        public override IStorageProvider Build()
        {
            var strategy = _pathStrategy != null
                ? _pathStrategy.Build()
                : new AppDataPathStrategy(DefaultSubdirectory);

            return new FileSystemStorageProvider(strategy);
        }
    }
}

using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "AppDataPathStrategy",
        menuName = "FLFloppa/Save System/Storage/Path Strategy/App Data",
        order = 0)]
    public sealed class AppDataPathStrategyAsset : StoragePathStrategyConfiguration
    {
        [SerializeField]
        [Tooltip("Optional subdirectory appended to Application.persistentDataPath.")]
        private string _subdirectory = "Saves";

        public override IStoragePathStrategy Build()
        {
            return new AppDataPathStrategy(_subdirectory);
        }
    }
}

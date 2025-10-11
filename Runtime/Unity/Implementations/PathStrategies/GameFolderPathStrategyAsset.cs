using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "GameFolderPathStrategy",
        menuName = "FLFloppa/Save System/Storage/Path Strategy/Game Folder",
        order = 1)]
    public sealed class GameFolderPathStrategyAsset : StoragePathStrategyConfiguration
    {
        [SerializeField]
        [Tooltip("Optional subdirectory appended to the game folder root.")]
        private string _subdirectory = "Saves";

        public override IStoragePathStrategy Build()
        {
            return new GameFolderPathStrategy(_subdirectory);
        }
    }
}

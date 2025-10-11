using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "CustomPathStrategy",
        menuName = "FLFloppa/Save System/Storage/Path Strategy/Custom",
        order = 2)]
    public sealed class CustomPathStrategyAsset : StoragePathStrategyConfiguration
    {
        [SerializeField]
        [Tooltip("Absolute or relative path to use for save data. Relative paths are resolved against the project/game root.")]
        private string _path = "Saves";

        public override IStoragePathStrategy Build()
        {
            return new CustomPathStrategy(_path);
        }
    }
}

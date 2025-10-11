using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "PlayerPrefsStorageProvider",
        menuName = "FLFloppa/Save System/Storage/PlayerPrefs Provider",
        order = 110)]
    public sealed class PlayerPrefsStorageProviderAsset : StorageProviderConfiguration
    {
        [SerializeField]
        [Tooltip("Optional prefix applied to every PlayerPrefs key (e.g. profile/category).")]
        private string _keyPrefix = "FLFloppa.Save.";

        public override IStorageProvider Build()
        {
            return new PlayerPrefsStorageProvider(_keyPrefix);
        }
    }
}

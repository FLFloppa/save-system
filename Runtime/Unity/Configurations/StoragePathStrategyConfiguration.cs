using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Base asset used to configure an <see cref="IStoragePathStrategy"/> for the file system storage provider.
    /// </summary>
    public abstract class StoragePathStrategyConfiguration : ScriptableObject
    {
        /// <summary>
        /// Creates the strategy instance used at runtime.
        /// </summary>
        /// <returns>Strategy implementation.</returns>
        public abstract IStoragePathStrategy Build();
    }
}

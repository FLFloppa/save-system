using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Base ScriptableObject for creating storage provider instances used by the save system.
    /// </summary>
    public abstract class StorageProviderConfiguration : ScriptableObject
    {
        /// <summary>
        /// Creates the runtime <see cref="IStorageProvider"/> implementation.
        /// </summary>
        /// <returns>Configured storage provider instance.</returns>
        public abstract IStorageProvider Build();
    }
}
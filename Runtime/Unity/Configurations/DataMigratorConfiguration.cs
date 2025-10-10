using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Base ScriptableObject for creating data migrator instances used to upgrade legacy saves.
    /// </summary>
    public abstract class DataMigratorConfiguration : ScriptableObject
    {
        /// <summary>
        /// Creates the runtime <see cref="IDataMigrator"/> implementation.
        /// </summary>
        /// <returns>Configured migrator instance.</returns>
        public abstract IDataMigrator Build();
    }
}
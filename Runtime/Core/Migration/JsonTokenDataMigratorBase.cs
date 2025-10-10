using System;
using Newtonsoft.Json.Linq;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Base class for JSON token migrations providing guard rails and cloning.
    /// </summary>
    public abstract class JsonTokenDataMigratorBase : IDataMigrator
    {
        public int FromVersion { get; }
        public int ToVersion { get; }

        protected JsonTokenDataMigratorBase(int fromVersion, int toVersion)
        {
            if (toVersion <= fromVersion)
            {
                throw new ArgumentException(
                    "`toVersion` must be greater than `fromVersion`.",
                    nameof(toVersion));
            }

            FromVersion = fromVersion;
            ToVersion = toVersion;
        }

        public JToken Migrate(JToken sourceData)
        {
            if (sourceData == null)
            {
                throw new SaveSystemException(
                    $"Migrator {GetType().Name} received a null JToken.");
            }

            var cloned = sourceData.DeepClone();
            var migrated = MigrateToken(cloned);

            if (migrated == null)
            {
                throw new SaveSystemException(
                    $"Migrator {GetType().Name} returned a null JToken.");
            }

            return migrated;
        }

        /// <summary>
        /// Override to provide migration logic. Source token is a deep clone of the original payload.
        /// </summary>
        protected abstract JToken MigrateToken(JToken source);
    }
}

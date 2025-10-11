using Newtonsoft.Json.Linq;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Represents a migration step capable of upgrading payloads between schema versions.
    /// </summary>
    public interface IDataMigrator
    {
        /// <summary>
        /// Gets the source version this migrator expects to receive.
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        /// Gets the target version that results after <see cref="Migrate"/> executes.
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        /// Converts the provided JSON token from <see cref="FromVersion"/> to <see cref="ToVersion"/>.
        /// </summary>
        /// <param name="sourceData">The serialized payload to transform.</param>
        /// <returns>The migrated payload token.</returns>
        JToken Migrate(JToken sourceData);
    }
}
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

    /// <summary>
    /// Defines serialization services for save payloads and envelopes.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes a CLR object graph into a byte array.
        /// </summary>
        /// <param name="data">The instance to serialize.</param>
        /// <returns>Serialized bytes representing <paramref name="data"/>.</returns>
        byte[] Serialize(object data);

        /// <summary>
        /// Deserializes bytes into an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The destination type.</typeparam>
        /// <param name="data">The serialized payload.</param>
        /// <returns>The reconstructed object.</returns>
        T Deserialize<T>(byte[] data);

        /// <summary>
        /// Deserializes bytes into a JSON token for migration or inspection purposes.
        /// </summary>
        /// <param name="data">The serialized payload.</param>
        /// <returns>The JSON token representation.</returns>
        JToken DeserializeToToken(byte[] data);

        /// <summary>
        /// Serializes a JSON token back into a byte array.
        /// </summary>
        /// <param name="token">The token to serialize.</param>
        /// <returns>Serialized representation of <paramref name="token"/>.</returns>
        byte[] SerializeFromToken(JToken token);
    }

    /// <summary>
    /// Provides persistence primitives for storing serialized save envelopes.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Writes the specified bytes to the backing store.
        /// </summary>
        /// <param name="key">The storage key (relative path).</param>
        /// <param name="data">The bytes to persist.</param>
        void Write(string key, byte[] data);

        /// <summary>
        /// Reads bytes previously persisted with <see cref="Write"/>.
        /// </summary>
        /// <param name="key">The storage key (relative path).</param>
        /// <returns>The stored bytes.</returns>
        byte[] Read(string key);

        /// <summary>
        /// Checks whether a save entry exists at the specified key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns><c>true</c> if the entry exists; otherwise, <c>false</c>.</returns>
        bool Exists(string key);

        /// <summary>
        /// Asynchronously writes bytes to the backing store.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="data">The bytes to persist.</param>
        /// <returns>A task representing the write operation.</returns>
        UniTask WriteAsync(string key, byte[] data);

        /// <summary>
        /// Asynchronously reads bytes for the specified key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A task producing the stored bytes.</returns>
        UniTask<byte[]> ReadAsync(string key);

        /// <summary>
        /// Asynchronously checks whether a save entry exists.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A task producing <c>true</c> if the entry exists.</returns>
        UniTask<bool> ExistsAsync(string key);
    }

    /// <summary>
    /// Represents a reversible processing step applied to save payloads (e.g., encryption, compression).
    /// </summary>
    public interface IProcessingModule
    {
        /// <summary>
        /// Processes bytes prior to persistence.
        /// </summary>
        /// <param name="input">The payload to transform.</param>
        /// <returns>The processed bytes.</returns>
        byte[] Process(byte[] input);

        /// <summary>
        /// Reverses <see cref="Process"/> during load.
        /// </summary>
        /// <param name="input">The processed bytes.</param>
        /// <returns>The restored payload bytes.</returns>
        byte[] Reverse(byte[] input);
    }

    /// <summary>
    /// Orchestrates a sequence of <see cref="IProcessingModule"/> instances for save/load flows.
    /// </summary>
    public interface IProcessingPipeline
    {
        /// <summary>
        /// Gets the configured modules in the pipeline.
        /// </summary>
        IReadOnlyList<IProcessingModule> AllModules { get; }

        /// <summary>
        /// Applies forward processing for save operations.
        /// </summary>
        /// <param name="data">Payload bytes to transform.</param>
        /// <returns>Processed bytes suitable for persistence.</returns>
        byte[] ProcessSave(byte[] data);

        /// <summary>
        /// Applies reverse processing during load operations.
        /// </summary>
        /// <param name="data">Stored bytes to restore.</param>
        /// <returns>Restored payload bytes.</returns>
        byte[] ProcessLoad(byte[] data);

        /// <summary>
        /// Returns the effective processing order for debugging or visualization.
        /// </summary>
        /// <returns>A snapshot of the processing chain.</returns>
        IReadOnlyList<IProcessingModule> GetProcessingChain();
    }

    /// <summary>
    /// Provides an abstraction over saving, loading, and managing save metadata.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Gets the active save profile name.
        /// </summary>
        string CurrentProfile { get; }

        /// <summary>
        /// Gets the active category name within the current profile.
        /// </summary>
        string CurrentCategory { get; }

        /// <summary>
        /// Sets the active profile used for subsequent save operations.
        /// </summary>
        /// <param name="profileName">The desired profile identifier.</param>
        void SetProfile(string profileName);

        /// <summary>
        /// Sets the active category used for subsequent save operations.
        /// </summary>
        /// <param name="categoryName">The desired category identifier.</param>
        void SetCategory(string categoryName);

        /// <summary>
        /// Synchronously saves data under the provided key.
        /// </summary>
        /// <typeparam name="T">The data type being saved.</typeparam>
        /// <param name="key">The unique key within the active profile/category.</param>
        /// <param name="data">The payload to persist.</param>
        /// <param name="metadata">Optional metadata to capture alongside the payload.</param>
        void Save<T>(string key, T data, IMetadata metadata = null);

        /// <summary>
        /// Synchronously loads save data for the provided key.
        /// </summary>
        /// <typeparam name="T">The expected payload type.</typeparam>
        /// <param name="key">The unique key within the active profile/category.</param>
        /// <returns>The loaded payload and associated metadata.</returns>
        SaveData<T> Load<T>(string key);

        /// <summary>
        /// Asynchronously saves data under the provided key.
        /// </summary>
        /// <typeparam name="T">The data type being saved.</typeparam>
        /// <param name="key">The unique key within the active profile/category.</param>
        /// <param name="data">The payload to persist.</param>
        /// <param name="metadata">Optional metadata to capture alongside the payload.</param>
        /// <returns>A task representing the save operation.</returns>
        UniTask SaveAsync<T>(string key, T data, IMetadata metadata = null);

        /// <summary>
        /// Asynchronously loads data for the provided key.
        /// </summary>
        /// <typeparam name="T">The expected payload type.</typeparam>
        /// <param name="key">The unique key within the active profile/category.</param>
        /// <returns>A task producing the loaded payload and metadata.</returns>
        UniTask<SaveData<T>> LoadAsync<T>(string key);
    }
}

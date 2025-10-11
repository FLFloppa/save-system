using Cysharp.Threading.Tasks;

namespace FLFloppa.SaveSystem
{
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
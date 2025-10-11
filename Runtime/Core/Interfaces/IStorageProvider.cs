using Cysharp.Threading.Tasks;

namespace FLFloppa.SaveSystem
{
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
}
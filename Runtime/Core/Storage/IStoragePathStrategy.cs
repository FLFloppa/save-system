using System;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Provides the root directory used by <see cref="FileSystemStorageProvider"/>.
    /// </summary>
    public interface IStoragePathStrategy
    {
        /// <summary>
        /// Gets the absolute path to the directory used for persistence.
        /// </summary>
        /// <returns>Root directory path.</returns>
        string GetPath();
    }
}

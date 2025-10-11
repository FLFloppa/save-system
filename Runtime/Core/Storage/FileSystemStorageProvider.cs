using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// File-system based storage provider with atomic primary/backup rotation.
    /// </summary>
    public sealed class FileSystemStorageProvider : IStorageProvider
    {
        private const string BackupExtension = ".bak";
        private const string TempExtension = ".tmp";

        private readonly string _rootPath;

        /// <summary>
        /// Gets the absolute root directory used to persist save files.
        /// </summary>
        public string RootPath => _rootPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemStorageProvider"/> class.
        /// </summary>
        /// <param name="pathStrategy">Strategy that resolves the root directory for save files.</param>
        public FileSystemStorageProvider(IStoragePathStrategy pathStrategy)
        {
            if (pathStrategy == null)
            {
                throw new ArgumentNullException(nameof(pathStrategy));
            }

            var rootPath = pathStrategy.GetPath();
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path cannot be null or whitespace.", nameof(pathStrategy));
            }

            _rootPath = Path.GetFullPath(rootPath);
        }

        /// <inheritdoc />
        public void Write(string key, byte[] data)
        {
            var paths = ResolvePaths(key);
            EnsureDirectory(paths.PrimaryPath);

            try
            {
                WriteTempFile(paths.TempPath, data);
                FinalizeWrite(paths);
            }
            catch (Exception ex)
            {
                CleanupTemp(paths.TempPath);
                RestorePrimaryFromBackup(paths);
                throw new SaveSystemException($"Failed to write save file '{paths.PrimaryPath}'.", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask WriteAsync(string key, byte[] data)
        {
            var paths = ResolvePaths(key);
            EnsureDirectory(paths.PrimaryPath);

            try
            {
                await WriteTempFileAsync(paths.TempPath, data);
                FinalizeWrite(paths);
            }
            catch (Exception ex)
            {
                CleanupTemp(paths.TempPath);
                RestorePrimaryFromBackup(paths);
                throw new SaveSystemException($"Failed to asynchronously write save file '{paths.PrimaryPath}'.", ex);
            }
        }

        /// <inheritdoc />
        public byte[] Read(string key)
        {
            var paths = ResolvePaths(key);

            try
            {
                if (File.Exists(paths.PrimaryPath))
                {
                    return File.ReadAllBytes(paths.PrimaryPath);
                }

                if (File.Exists(paths.BackupPath))
                {
                    return File.ReadAllBytes(paths.BackupPath);
                }

                throw new SaveSystemException($"Save file '{paths.PrimaryPath}' does not exist.");
            }
            catch (SaveSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to read save file '{paths.PrimaryPath}'.", ex);
            }
        }

        /// <inheritdoc />
        public async UniTask<byte[]> ReadAsync(string key)
        {
            var paths = ResolvePaths(key);

            try
            {
                if (File.Exists(paths.PrimaryPath))
                {
                    return await File.ReadAllBytesAsync(paths.PrimaryPath);
                }

                if (File.Exists(paths.BackupPath))
                {
                    return await File.ReadAllBytesAsync(paths.BackupPath);
                }

                throw new SaveSystemException($"Save file '{paths.PrimaryPath}' does not exist.");
            }
            catch (SaveSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to asynchronously read save file '{paths.PrimaryPath}'.", ex);
            }
        }

        /// <inheritdoc />
        public bool Exists(string key)
        {
            var paths = ResolvePaths(key);
            return File.Exists(paths.PrimaryPath) || File.Exists(paths.BackupPath);
        }

        /// <inheritdoc />
        public async UniTask<bool> ExistsAsync(string key)
        {
            return Exists(key);
        }

        /// <summary>
        /// Resolves absolute primary, backup, and temporary file paths for the given storage key.
        /// </summary>
        /// <param name="key">Relative storage key (may contain directory separators).</param>
        /// <returns>A tuple containing the primary, backup, and temporary paths.</returns>
        private (string PrimaryPath, string BackupPath, string TempPath) ResolvePaths(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SaveSystemException("Storage key cannot be null or empty.");
            }

            var normalizedKey = key.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var primary = Path.Combine(_rootPath, normalizedKey);
            var backup = primary + BackupExtension;
            var temp = primary + TempExtension;
            return (primary, backup, temp);
        }

        /// <summary>
        /// Ensures the directory for the specified save path exists.
        /// </summary>
        /// <param name="primaryPath">Primary save file path.</param>
        private static void EnsureDirectory(string primaryPath)
        {
            var directory = Path.GetDirectoryName(primaryPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new SaveSystemException(
                    $"Unable to determine directory for save file '{primaryPath}'.");
            }

            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Writes bytes to a temporary file, performing validation and cleanup of prior temp files.
        /// </summary>
        /// <param name="tempPath">Temporary file path.</param>
        /// <param name="data">Bytes to persist.</param>
        private static void WriteTempFile(string tempPath, byte[] data)
        {
            CleanupTemp(tempPath);

            using var fileStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            fileStream.Write(data, 0, data.Length);
            fileStream.Flush(true);

            VerifyWrite(tempPath, data.Length);
        }

        /// <summary>
        /// Asynchronously writes bytes to a temporary file, performing validation and cleanup of prior temp files.
        /// </summary>
        /// <param name="tempPath">Temporary file path.</param>
        /// <param name="data">Bytes to persist.</param>
        private static async UniTask WriteTempFileAsync(string tempPath, byte[] data)
        {
            CleanupTemp(tempPath);

            await using var fileStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await fileStream.WriteAsync(data, 0, data.Length);
            await fileStream.FlushAsync();

            VerifyWrite(tempPath, data.Length);
        }

        /// <summary>
        /// Performs a sanity check to ensure the written file exists and matches the expected size.
        /// </summary>
        /// <param name="path">Path to the file on disk.</param>
        /// <param name="expectedLength">Expected byte length.</param>
        private static void VerifyWrite(string path, int expectedLength)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length != expectedLength)
            {
                throw new SaveSystemException(
                    $"Written file '{path}' length mismatch (expected {expectedLength}, actual {info.Length}).");
            }
        }

        /// <summary>
        /// Replaces the primary save file atomically, rotating previous versions into a backup.
        /// </summary>
        /// <param name="paths">Tuple containing primary, backup, and temporary file paths.</param>
        private static void FinalizeWrite((string PrimaryPath, string BackupPath, string TempPath) paths)
        {
            if (File.Exists(paths.BackupPath))
            {
                File.Delete(paths.BackupPath);
            }

            if (File.Exists(paths.PrimaryPath))
            {
                File.Move(paths.PrimaryPath, paths.BackupPath);
            }

            File.Move(paths.TempPath, paths.PrimaryPath);
        }

        /// <summary>
        /// Deletes any existing temporary file for the specified path.
        /// </summary>
        /// <param name="tempPath">Path to the temporary file.</param>
        private static void CleanupTemp(string tempPath)
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        /// <summary>
        /// Attempts to restore a missing primary file from its backup counterpart.
        /// </summary>
        /// <param name="paths">Tuple containing primary, backup, and temporary file paths.</param>
        private static void RestorePrimaryFromBackup((string PrimaryPath, string BackupPath, string TempPath) paths)
        {
            try
            {
                if (!File.Exists(paths.PrimaryPath) && File.Exists(paths.BackupPath))
                {
                    File.Move(paths.BackupPath, paths.PrimaryPath);
                }
            }
            catch (Exception)
            {
                // Swallow exceptions during best-effort restore to avoid masking the original error.
            }
        }
    }
}

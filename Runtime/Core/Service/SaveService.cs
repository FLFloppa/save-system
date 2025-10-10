using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Pure C# implementation orchestrating serialization, processing, storage, and migration.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        private const string DefaultProfileName = "DefaultProfile";
        private const string DefaultCategoryName = "DefaultCategory";

        private static readonly HashSet<char> InvalidFileNameCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());

        private readonly int _currentVersion;
        private readonly ISerializer _serializer;
        private readonly IStorageProvider _storageProvider;
        private readonly IProcessingPipeline _processingPipeline;
        private readonly IReadOnlyDictionary<int, IDataMigrator> _migrators;
        private readonly string _fileExtension;

        private string _activeProfile;
        private string _activeCategory;

        /// <summary>
        /// Gets the active save profile name used for relative storage paths.
        /// </summary>
        public string CurrentProfile => _activeProfile;

        /// <summary>
        /// Gets the active category name nested within the current profile.
        /// </summary>
        public string CurrentCategory => _activeCategory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveService"/> class.
        /// </summary>
        /// <param name="currentVersion">The current schema version expected for saves.</param>
        /// <param name="serializer">Serializer used for envelopes and payloads.</param>
        /// <param name="storageProvider">Provider responsible for reading/writing bytes.</param>
        /// <param name="processingPipeline">Pipeline that processes payloads during save/load.</param>
        /// <param name="migrators">Migrators used to upgrade older payload versions.</param>
        /// <param name="fileExtension">File extension appended to save files.</param>
        /// <param name="defaultProfile">Initial profile name used for storage.</param>
        /// <param name="defaultCategory">Initial category name used for storage.</param>
        public SaveService(
            int currentVersion,
            ISerializer serializer,
            IStorageProvider storageProvider,
            IProcessingPipeline processingPipeline,
            IReadOnlyDictionary<int, IDataMigrator> migrators,
            string fileExtension = ".sav",
            string defaultProfile = DefaultProfileName,
            string defaultCategory = DefaultCategoryName)
        {
            _currentVersion = currentVersion;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _processingPipeline = processingPipeline ?? throw new ArgumentNullException(nameof(processingPipeline));
            _migrators = migrators ?? new Dictionary<int, IDataMigrator>();
            _fileExtension = NormalizeExtension(fileExtension);

            _activeProfile = SanitizeSegment(defaultProfile ?? DefaultProfileName, nameof(defaultProfile));
            _activeCategory = SanitizeSegment(defaultCategory ?? DefaultCategoryName, nameof(defaultCategory));
        }

        /// <summary>
        /// Changes the active profile used when composing file system paths.
        /// </summary>
        /// <param name="profileName">The profile identifier to switch to.</param>
        public void SetProfile(string profileName)
        {
            _activeProfile = SanitizeSegment(profileName, nameof(profileName));
        }

        /// <summary>
        /// Changes the active category used when composing file system paths.
        /// </summary>
        /// <param name="categoryName">The category identifier to switch to.</param>
        public void SetCategory(string categoryName)
        {
            _activeCategory = SanitizeSegment(categoryName, nameof(categoryName));
        }

        /// <summary>
        /// Synchronously saves the specified payload and optional metadata.
        /// </summary>
        /// <typeparam name="T">Type of the payload being saved.</typeparam>
        /// <param name="key">Unique key within the active profile/category.</param>
        /// <param name="data">Payload to serialize and store.</param>
        /// <param name="metadata">Optional metadata bundled with the payload.</param>
        public void Save<T>(string key, T data, IMetadata metadata = null)
        {
            var path = BuildRelativePath(key);

            try
            {
                var envelopeBytes = BuildEnvelopeBytes(data, metadata);
                _storageProvider.Write(path, envelopeBytes);
            }
            catch (SaveSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to save key '{key}'.", ex);
            }
        }

        /// <summary>
        /// Asynchronously saves the specified payload and optional metadata.
        /// </summary>
        /// <typeparam name="T">Type of the payload being saved.</typeparam>
        /// <param name="key">Unique key within the active profile/category.</param>
        /// <param name="data">Payload to serialize and store.</param>
        /// <param name="metadata">Optional metadata bundled with the payload.</param>
        public async UniTask SaveAsync<T>(string key, T data, IMetadata metadata = null)
        {
            var path = BuildRelativePath(key);

            try
            {
                var envelopeBytes = BuildEnvelopeBytes(data, metadata);
                await _storageProvider.WriteAsync(path, envelopeBytes);
            }
            catch (SaveSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to asynchronously save key '{key}'.", ex);
            }
        }

        /// <summary>
        /// Synchronously loads payload and metadata for the specified key.
        /// </summary>
        /// <typeparam name="T">Expected payload type.</typeparam>
        /// <param name="key">Unique key within the active profile/category.</param>
        /// <returns>The loaded payload and associated metadata.</returns>
        public SaveData<T> Load<T>(string key)
        {
            var path = BuildRelativePath(key);

            try
            {
                if (!_storageProvider.Exists(path))
                {
                    throw new SaveSystemException($"Save entry '{path}' does not exist.");
                }

                var envelopeBytes = _storageProvider.Read(path);
                return DeserializeSaveData<T>(envelopeBytes);
            }
            catch (SaveSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to load key '{key}'.", ex);
            }
        }

        /// <summary>
        /// Asynchronously loads payload and metadata for the specified key.
        /// </summary>
        /// <typeparam name="T">Expected payload type.</typeparam>
        /// <param name="key">Unique key within the active profile/category.</param>
        /// <returns>A task producing the loaded payload and metadata.</returns>
        public async UniTask<SaveData<T>> LoadAsync<T>(string key)
        {
            var path = BuildRelativePath(key);

            try
            {
                if (!await _storageProvider.ExistsAsync(path))
                {
                    throw new SaveSystemException($"Save entry '{path}' does not exist.");
                }

                var envelopeBytes = await _storageProvider.ReadAsync(path);
                return DeserializeSaveData<T>(envelopeBytes);
            }
            catch (SaveSystemException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SaveSystemException($"Failed to asynchronously load key '{key}'.", ex);
            }
        }

        private byte[] BuildEnvelopeBytes<T>(T data, IMetadata metadata)
        {
            if (data == null)
            {
                throw new SaveSystemException("Cannot serialize null save data.");
            }

            var serializedData = _serializer.Serialize(data);
            var processedData = _processingPipeline.ProcessSave(serializedData);

            var envelope = new SaveEnvelope
            {
                Version = _currentVersion,
                Metadata = metadata,
                Payload = processedData,
                Readable = ReadableUtility.CreateReadableSummary(data, metadata)
            };

            return _serializer.Serialize(envelope);
        }

        private SaveData<T> DeserializeSaveData<T>(byte[] envelopeBytes)
        {
            if (envelopeBytes == null || envelopeBytes.Length == 0)
            {
                throw new SaveSystemException("Save data is empty.");
            }

            var envelope = _serializer.Deserialize<SaveEnvelope>(envelopeBytes)
                ?? throw new SaveSystemException("Failed to deserialize save envelope.");

            var payloadBytes = PreparePayloadForLoad(envelope);
            var data = _serializer.Deserialize<T>(payloadBytes);

            return new SaveData<T>(data, envelope.Metadata);
        }

        private byte[] PreparePayloadForLoad(SaveEnvelope envelope)
        {
            if (envelope.Version > _currentVersion)
            {
                throw new SaveSystemException(
                    $"Save version {envelope.Version} is newer than expected {_currentVersion}.");
            }

            var payload = envelope.Payload ?? Array.Empty<byte>();

            if (envelope.Version < _currentVersion)
            {
                payload = MigratePayload(envelope.Version, payload);
            }

            return _processingPipeline.ProcessLoad(payload);
        }

        private byte[] MigratePayload(int sourceVersion, byte[] payload)
        {
            if (payload == null)
            {
                throw new SaveSystemException("Cannot migrate a null payload.");
            }

            var token = _serializer.DeserializeToToken(payload)
                ?? throw new SaveSystemException("Failed to deserialize payload for migration.");

            var versionCursor = sourceVersion;

            while (versionCursor < _currentVersion)
            {
                if (!_migrators.TryGetValue(versionCursor, out var migrator))
                {
                    throw new SaveSystemException(
                        $"No migrator registered for version {versionCursor}.");
                }

                var migratedToken = migrator.Migrate(token)
                    ?? throw new SaveSystemException(
                        $"Migrator '{migrator.GetType().Name}' returned a null token.");

                if (migrator.ToVersion <= versionCursor)
                {
                    throw new SaveSystemException(
                        $"Migrator '{migrator.GetType().Name}' does not advance the version.");
                }

                token = migratedToken;
                versionCursor = migrator.ToVersion;
            }

            if (versionCursor != _currentVersion)
            {
                throw new SaveSystemException(
                    $"Migration ended at version {versionCursor}, expected {_currentVersion}.");
            }

            return _serializer.SerializeFromToken(token);
        }

        private string BuildRelativePath(string key)
        {
            var sanitizedKey = SanitizeSegment(key, nameof(key));
            return $"{_activeProfile}/{_activeCategory}/{sanitizedKey}{_fileExtension}";
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return ".sav";
            }

            extension = extension.Trim();
            if (!extension.StartsWith(".", StringComparison.Ordinal))
            {
                extension = "." + extension;
            }

            return extension;
        }

        private static string SanitizeSegment(string segment, string segmentName)
        {
            if (segment == null)
            {
                throw new SaveSystemException($"{segmentName} cannot be null.");
            }

            var trimmed = segment.Trim();
            if (trimmed.Length == 0)
            {
                throw new SaveSystemException($"{segmentName} cannot be empty.");
            }

            var characters = trimmed.ToCharArray();
            for (var i = 0; i < characters.Length; i++)
            {
                if (InvalidFileNameCharacters.Contains(characters[i]) || characters[i] == Path.DirectorySeparatorChar || characters[i] == Path.AltDirectorySeparatorChar)
                {
                    characters[i] = '_';
                }
            }

            var sanitized = new string(characters).Trim();
            if (sanitized.Length == 0)
            {
                throw new SaveSystemException(
                    $"{segmentName} results in an empty name after sanitization.");
            }

            return sanitized;
        }
    }
}

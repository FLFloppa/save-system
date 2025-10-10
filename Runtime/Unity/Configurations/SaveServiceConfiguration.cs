using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// ScriptableObject used to configure and build runtime save services.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SaveServiceConfiguration",
        menuName = "FLFloppa/Save System/Save Service Configuration",
        order = 0)]
    public class SaveServiceConfiguration : ScriptableObject
    {
        [SerializeField]
        [Tooltip("The current version of the save data structure.")]
        private int _currentVersion = 1;

        [Header("Core Components")]
        [SerializeField]
        private SerializerConfiguration _serializer;

        [SerializeField]
        private StorageProviderConfiguration _storageProvider;

        [SerializeField]
        private ProcessingPipelineConfiguration _processingPipeline;

        [Header("Data Migration")]
        [SerializeField]
        [Tooltip("A list of all migrators. They will be chained automatically.")]
        private List<DataMigratorConfiguration> _migrators = new();

        /// <summary>
        /// Gets the current save schema version this configuration expects.
        /// </summary>
        public int CurrentVersion => _currentVersion;

        /// <summary>
        /// Gets the serializer configuration used to encode payloads and envelopes.
        /// </summary>
        public SerializerConfiguration Serializer => _serializer;

        /// <summary>
        /// Gets the storage provider configuration used to persist save data.
        /// </summary>
        public StorageProviderConfiguration StorageProvider => _storageProvider;

        /// <summary>
        /// Gets the processing pipeline configuration applied to payloads.
        /// </summary>
        public ProcessingPipelineConfiguration ProcessingPipeline => _processingPipeline;

        /// <summary>
        /// Gets the ordered list of migrator configurations used to upgrade legacy saves.
        /// </summary>
        public IReadOnlyList<DataMigratorConfiguration> Migrators => _migrators;

        /// <summary>
        /// Builds a runtime <see cref="ISaveService"/> instance using the configured components.
        /// </summary>
        /// <returns>The constructed save service.</returns>
        /// <exception cref="SaveSystemException">Thrown when required components are missing.</exception>
        public virtual ISaveService Build()
        {
            if (_serializer == null)
            {
                throw new SaveSystemException("Serializer configuration is not assigned.");
            }

            if (_storageProvider == null)
            {
                throw new SaveSystemException("Storage provider configuration is not assigned.");
            }

            if (_processingPipeline == null)
            {
                throw new SaveSystemException("Processing pipeline configuration is not assigned.");
            }

            var serializer = _serializer.Build();
            var storageProvider = _storageProvider.Build();
            var processingPipeline = _processingPipeline.Build();
            var migratorMap = BuildMigratorDictionary();

            return CreateService(
                _currentVersion,
                serializer,
                storageProvider,
                processingPipeline,
                migratorMap);
        }

        /// <summary>
        /// Creates the runtime save service. Override to provide custom implementations.
        /// </summary>
        /// <param name="currentVersion">Current schema version.</param>
        /// <param name="serializer">Serializer instance.</param>
        /// <param name="storageProvider">Storage provider instance.</param>
        /// <param name="processingPipeline">Processing pipeline instance.</param>
        /// <param name="migratorMap">Map of migrators keyed by source version.</param>
        /// <returns>Constructed save service.</returns>
        protected virtual ISaveService CreateService(
            int currentVersion,
            ISerializer serializer,
            IStorageProvider storageProvider,
            IProcessingPipeline processingPipeline,
            IReadOnlyDictionary<int, IDataMigrator> migratorMap)
        {
            return new SaveService(
                currentVersion,
                serializer,
                storageProvider,
                processingPipeline,
                migratorMap);
        }

        /// <summary>
        /// Builds a dictionary of data migrators keyed by their source version.
        /// </summary>
        /// <returns>Read-only dictionary of migrators.</returns>
        protected virtual IReadOnlyDictionary<int, IDataMigrator> BuildMigratorDictionary()
        {
            var built = new Dictionary<int, IDataMigrator>();

            if (_migrators == null || _migrators.Count == 0)
            {
                return new ReadOnlyDictionary<int, IDataMigrator>(built);
            }

            foreach (var config in _migrators.Where(m => m != null))
            {
                var migrator = config.Build();
                if (migrator == null)
                {
                    continue;
                }

                if (built.ContainsKey(migrator.FromVersion))
                {
                    throw new SaveSystemException(
                        $"Duplicate migrator for version {migrator.FromVersion} detected.");
                }

                built[migrator.FromVersion] = migrator;
            }

            return new ReadOnlyDictionary<int, IDataMigrator>(built);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_currentVersion < 1)
            {
                _currentVersion = 1;
            }
        }

        /// <summary>
        /// Builds the migrator dictionary for editor workflows.
        /// </summary>
        /// <returns>Read-only dictionary of migrators.</returns>
        public IReadOnlyDictionary<int, IDataMigrator> BuildMigratorDictionaryForEditor()
        {
            return BuildMigratorDictionary();
        }
#endif
    }
}

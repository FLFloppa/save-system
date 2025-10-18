using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FLFloppa.EditorHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    /// <summary>
    /// Editor window that inspects on-disk save files, showing metadata, payload previews, and tooling shortcuts.
    /// </summary>
    public sealed class SaveObserverWindow : EditorWindow
    {
        private const string WindowTitle = "Save Observer";

        [MenuItem("FLFloppa/Save System/Save Observer", priority = 310)]
        /// <summary>
        /// Opens the Save Observer window via the Unity menu.
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<SaveObserverWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(420, 360);
            window.Show();
        }

        /// <summary>
        /// Opens the Save Observer window and queues a configuration for immediate inspection.
        /// </summary>
        /// <param name="configuration">Configuration used to locate and decode saves.</param>
        public static void ShowWindow(SaveServiceConfiguration configuration)
        {
            var window = GetWindow<SaveObserverWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(420, 360);
            window._queuedConfiguration = configuration;
            window.Show();
        }

        private ObjectField _configurationField;
        private ToolbarSearchField _searchField;
        private HelpBox _statusBox;
        private ScrollView _entriesScroll;

        private SaveServiceConfiguration _queuedConfiguration;
        private string _searchFilter = string.Empty;
        private readonly List<string> _warnings = new List<string>();

        /// <summary>
        /// Unity callback that builds the window's UI Toolkit layout.
        /// </summary>
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;

            var contentRoot = InspectorUi.Layout.CreateRoot();
            contentRoot.style.flexGrow = 1f;
            root.Add(contentRoot);

            var configCard = InspectorUi.Cards.Create(
                "Configuration",
                out var configContent,
                "Select a configuration and manage observed save data.");

            _configurationField = new ObjectField("Configuration")
            {
                objectType = typeof(SaveServiceConfiguration)
            };
            _configurationField.style.flexGrow = 1f;
            _configurationField.style.minWidth = 200;
            _configurationField.RegisterValueChangedCallback(_ => RefreshEntries());
            configContent.Add(_configurationField);

            var controlsRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginTop = InspectorUi.Layout.ButtonSpacing
                }
            };
            controlsRow.Add(InspectorUi.Controls.CreateActionButton("Refresh", RefreshEntries));

            _searchField = new ToolbarSearchField();
            _searchField.style.flexGrow = 1f;
            _searchField.style.minWidth = 160;
            _searchField.style.marginLeft = InspectorUi.Layout.ButtonSpacing;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue?.Trim() ?? string.Empty;
                RebuildContent(_cachedEntries);
            });
            controlsRow.Add(_searchField);

            configContent.Add(controlsRow);
            contentRoot.Add(configCard);

            var statusCard = InspectorUi.Cards.Create(
                "Status",
                out var statusContent,
                "Live observer status and warnings.");
            _statusBox = InspectorUi.Controls.AddHelpBox(statusContent, string.Empty, HelpBoxMessageType.Info);
            _statusBox.style.display = DisplayStyle.None;
            contentRoot.Add(statusCard);

            var entriesCard = InspectorUi.Cards.Create(
                "Entries",
                out var entriesContent,
                "Browse saved data grouped by profile and category.");
            _entriesScroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1f,
                    flexDirection = FlexDirection.Column
                }
            };
            entriesContent.Add(_entriesScroll);
            contentRoot.Add(entriesCard);

            if (_queuedConfiguration != null)
            {
                _configurationField.value = _queuedConfiguration;
                _queuedConfiguration = null;
                RefreshEntries();
            }
        }

        private readonly List<SaveEntry> _cachedEntries = new List<SaveEntry>();

        /// <summary>
        /// Reloads save entries from disk using the selected configuration.
        /// </summary>
        private void RefreshEntries()
        {
            _cachedEntries.Clear();
            _warnings.Clear();
            _entriesScroll?.Clear();
            SetStatus(string.Empty);

            var configuration = _configurationField.value as SaveServiceConfiguration;
            if (configuration == null)
            {
                SetStatus("Select a `SaveServiceConfiguration` to inspect saves.", HelpBoxMessageType.Info);
                return;
            }

            if (configuration.Serializer == null)
            {
                SetStatus("Configuration is missing a serializer asset.", HelpBoxMessageType.Error);
                return;
            }

            if (configuration.StorageProvider == null)
            {
                SetStatus("Configuration is missing a storage provider asset.", HelpBoxMessageType.Error);
                return;
            }

            ISerializer serializer;
            try
            {
                serializer = configuration.Serializer.Build();
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to build serializer: {ex.Message}", HelpBoxMessageType.Error);
                return;
            }

            FileSystemStorageProvider fileSystemProvider = null;
            try
            {
                var provider = configuration.StorageProvider.Build();
                fileSystemProvider = provider as FileSystemStorageProvider;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to build storage provider: {ex.Message}", HelpBoxMessageType.Error);
                return;
            }

            if (fileSystemProvider == null)
            {
                SetStatus("Save Observer currently supports `FileSystemStorageProvider` only.", HelpBoxMessageType.Warning);
                return;
            }

            var rootPath = fileSystemProvider.RootPath;
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                SetStatus("Storage root path is empty.", HelpBoxMessageType.Warning);
                return;
            }

            if (!Directory.Exists(rootPath))
            {
                SetStatus($"Storage directory does not exist yet: {rootPath}", HelpBoxMessageType.Info);
                return;
            }

            var context = new InspectionContext
            {
                Serializer = serializer,
                Pipeline = TryBuildPipeline(configuration.ProcessingPipeline, _warnings),
                Migrators = TryBuildMigrators(configuration, _warnings),
                CurrentVersion = configuration.CurrentVersion
            };

            try
            {
                foreach (var entry in EnumerateEntries(rootPath, context))
                {
                    _cachedEntries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to enumerate saves: {ex.Message}", HelpBoxMessageType.Error);
                return;
            }

            if (_cachedEntries.Count == 0)
            {
                SetStatus("No save files found in the storage directory.", HelpBoxMessageType.Info);
                DisplayWarnings();
                return;
            }

            RebuildContent(_cachedEntries);
            DisplayWarnings();
        }

        /// <summary>
        /// Rebuilds the UI content displays for the provided entries.
        /// </summary>
        /// <param name="entries">Entries to display.</param>
        private void RebuildContent(IEnumerable<SaveEntry> entries)
        {
            _entriesScroll?.Clear();

            var filter = string.IsNullOrEmpty(_searchFilter)
                ? null
                : _searchFilter.ToLowerInvariant();

            var grouped = entries
                .Where(e => filter == null || e.Key.ToLowerInvariant().Contains(filter) || e.Profile.ToLowerInvariant().Contains(filter) || e.Category.ToLowerInvariant().Contains(filter))
                .GroupBy(e => e.Profile)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            if (!grouped.Any())
            {
                var message = string.IsNullOrEmpty(_searchFilter)
                    ? "No save files found in the storage directory."
                    : "No saves match the current search filter.";
                SetStatus(message, HelpBoxMessageType.Info);
                return;
            }

            SetStatus(string.Empty);

            foreach (var profileGroup in grouped)
            {
                var profileCard = InspectorUi.Cards.Create($"Profile: {profileGroup.Key}", out var profileContent);

                foreach (var categoryGroup in profileGroup
                             .GroupBy(e => e.Category)
                             .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var categoryFoldout = new Foldout
                    {
                        text = $"Category: {categoryGroup.Key}",
                        value = false,
                        style =
                        {
                            marginBottom = 4
                        }
                    };
                    categoryFoldout.style.marginLeft = 4;

                    foreach (var entry in categoryGroup.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        categoryFoldout.Add(BuildEntryElement(entry));
                    }

                    profileContent.Add(categoryFoldout);
                }

                _entriesScroll.Add(profileCard);
            }
        }

        /// <summary>
        /// Creates a card visual element for a specific save entry.
        /// </summary>
        /// <param name="entry">Save entry metadata.</param>
        /// <returns>UI element representing the entry.</returns>
        private VisualElement BuildEntryElement(SaveEntry entry)
        {
            var card = InspectorUi.Cards.Create(entry.Key, out var content, entry.RelativePath);

            content.Add(InspectorUi.Controls.CreateSummaryLabel($"{EditorUtility.FormatBytes(entry.FileSize)} · {entry.LastWriteTime:G}"));

            if (entry.Error != null)
            {
                InspectorUi.Controls.AddHelpBox(content, entry.Error.Message, HelpBoxMessageType.Error);
                content.Add(BuildFooter(entry));
                return card;
            }

            if (!string.IsNullOrWhiteSpace(entry.Envelope?.Readable))
            {
                content.Add(new Label(entry.Envelope.Readable)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                        marginBottom = 4
                    }
                });
            }

            if (entry.DecodeError != null)
            {
                InspectorUi.Controls.AddHelpBox(
                    content,
                    $"Failed to decode payload: {entry.DecodeError.Message}",
                    HelpBoxMessageType.Warning);
            }
            else if (!string.IsNullOrEmpty(entry.DataPreview))
            {
                var previewFoldout = new Foldout
                {
                    text = "Data Preview",
                    value = false,
                    style =
                    {
                        marginBottom = 4
                    }
                };

                var previewField = new TextField
                {
                    multiline = true,
                    value = entry.DataPreview,
                    isReadOnly = true
                };
                previewField.style.whiteSpace = WhiteSpace.Pre;
                previewField.style.maxHeight = 220;
                previewFoldout.Add(previewField);
                content.Add(previewFoldout);
            }

            if (entry.Envelope?.Metadata != null)
            {
                if (ReadableUtility.TryGetReadableElement(entry.Envelope.Metadata, out var metadataElement))
                {
                    metadataElement.style.marginBottom = 4;
                    content.Add(metadataElement);
                }
                else if (ReadableUtility.TryGetReadableString(entry.Envelope.Metadata, out var metadataReadable))
                {
                    InspectorUi.Controls.AddHelpBox(
                        content,
                        metadataReadable,
                        HelpBoxMessageType.Info);
                }
                else
                {
                    content.Add(new Label(entry.Envelope.Metadata.ToString())
                    {
                        style =
                        {
                            whiteSpace = WhiteSpace.Normal,
                            marginBottom = 4
                        }
                    });
                }
            }

            content.Add(BuildFooter(entry));
            return card;
        }

        /// <summary>
        /// Builds the footer toolbar for a save entry card.
        /// </summary>
        /// <param name="entry">Save entry metadata.</param>
        /// <returns>Footer element with action buttons.</returns>
        private VisualElement BuildFooter(SaveEntry entry)
        {
            var footer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    marginTop = 2
                }
            };

            var revealButton = new Button(() => EditorUtility.RevealInFinder(entry.FilePath))
            {
                text = "Show In Explorer"
            };
            revealButton.style.marginRight = 4;
            footer.Add(revealButton);

            var copyPathButton = new Button(() => EditorGUIUtility.systemCopyBuffer = entry.FilePath)
            {
                text = "Copy Path"
            };
            copyPathButton.style.marginRight = 4;
            footer.Add(copyPathButton);

            if (!string.IsNullOrEmpty(entry.Envelope?.Readable))
            {
                var copyReadableButton = new Button(() => EditorGUIUtility.systemCopyBuffer = entry.Envelope.Readable)
                {
                    text = "Copy Summary"
                };
                copyReadableButton.style.marginRight = 4;
                footer.Add(copyReadableButton);
            }

            if (!string.IsNullOrEmpty(entry.DataPreview))
            {
                var copyDataButton = new Button(() => EditorGUIUtility.systemCopyBuffer = entry.DataPreview)
                {
                    text = "Copy Data Preview"
                };
                footer.Add(copyDataButton);
            }

            return footer;
        }

        /// <summary>
        /// Enumerates save files on disk, yielding structured save entries.
        /// </summary>
        /// <param name="rootPath">Root directory containing saves.</param>
        /// <param name="context">Inspection context used to decode envelopes.</param>
        /// <returns>Enumerable sequence of save entries.</returns>
        private IEnumerable<SaveEntry> EnumerateEntries(string rootPath, InspectionContext context)
        {
            var files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var relativePath = Path.GetRelativePath(rootPath, file);
                var segments = relativePath
                    .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                var profile = segments.Length > 0 ? segments[0] : "(root)";
                var category = segments.Length > 2 ? segments[1] : segments.Length > 1 ? segments[1] : "(root)";
                var key = Path.GetFileNameWithoutExtension(file);

                SaveEnvelope envelope = null;
                Exception error = null;

                try
                {
                    var bytes = File.ReadAllBytes(file);
                    envelope = context.Serializer.Deserialize<SaveEnvelope>(bytes);
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                var entry = new SaveEntry
                {
                    Profile = profile,
                    Category = category,
                    Key = key,
                    FilePath = file,
                    RelativePath = relativePath,
                    FileSize = info.Length,
                    LastWriteTime = info.LastWriteTime,
                    Envelope = envelope,
                    Error = error
                };

                if (error == null && envelope != null)
                {
                    PopulateEntry(entry, context);
                }

                yield return entry;
            }
        }

        /// <summary>
        /// Sets the window status label to the provided message.
        /// </summary>
        /// <param name="message">Status text (hidden when empty).</param>
        private void SetStatus(string message, HelpBoxMessageType type = HelpBoxMessageType.Info)
        {
            if (_statusBox == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                _statusBox.style.display = DisplayStyle.None;
                _statusBox.text = string.Empty;
                return;
            }

            _statusBox.style.display = DisplayStyle.Flex;
            _statusBox.messageType = type;
            _statusBox.text = message;
        }

        /// <summary>
        /// Appends any collected warnings to the status label.
        /// </summary>
        private void DisplayWarnings()
        {
            if (_warnings.Count == 0)
            {
                return;
            }

            var warningText = string.Join("\n", _warnings);
            if (_statusBox == null || _statusBox.style.display == DisplayStyle.None)
            {
                SetStatus(warningText, HelpBoxMessageType.Warning);
            }
            else
            {
                SetStatus(_statusBox.text + "\n" + warningText, HelpBoxMessageType.Warning);
            }
        }

        /// <summary>
        /// Attempts to build the processing pipeline, logging warnings if creation fails.
        /// </summary>
        /// <param name="pipelineConfig">Pipeline configuration asset.</param>
        /// <param name="warnings">Warning list to populate on failure.</param>
        /// <returns>Built pipeline or <c>null</c> if creation failed.</returns>
        private static IProcessingPipeline TryBuildPipeline(ProcessingPipelineConfiguration pipelineConfig, IList<string> warnings)
        {
            if (pipelineConfig == null)
            {
                return null;
            }

            try
            {
                return pipelineConfig.Build();
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to build processing pipeline: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds the migrator dictionary for editor use, logging warnings if creation fails.
        /// </summary>
        /// <param name="configuration">Save service configuration.</param>
        /// <param name="warnings">Warning list to populate on failure.</param>
        /// <returns>Dictionary mapping from version to migrator.</returns>
        private static IReadOnlyDictionary<int, IDataMigrator> TryBuildMigrators(SaveServiceConfiguration configuration, IList<string> warnings)
        {
            try
            {
                return configuration.BuildMigratorDictionaryForEditor();
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to build migrator map: {ex.Message}");
                return new ReadOnlyDictionary<int, IDataMigrator>(new Dictionary<int, IDataMigrator>());
            }
        }

        /// <summary>
        /// Populates decoded payload information and previews for the provided save entry.
        /// </summary>
        /// <param name="entry">Entry to update.</param>
        /// <param name="context">Inspection context.</param>
        private static void PopulateEntry(SaveEntry entry, InspectionContext context)
        {
            try
            {
                var payload = entry.Envelope.Payload ?? Array.Empty<byte>();
                var processed = context.Pipeline != null
                    ? context.Pipeline.ProcessLoad(payload)
                    : payload;

                if (entry.Envelope.Version < context.CurrentVersion)
                {
                    processed = ApplyMigrations(processed, entry.Envelope.Version, context);
                }

                var token = context.Serializer.DeserializeToToken(processed);
                entry.DataPreview = token?.ToString(Formatting.Indented) ?? string.Empty;
            }
            catch (Exception ex)
            {
                entry.DecodeError = ex;
            }
        }

        private static byte[] ApplyMigrations(byte[] payload, int version, InspectionContext context)
        {
            if (context.Migrators == null || context.Migrators.Count == 0)
            {
                return payload;
            }

            var token = context.Serializer.DeserializeToToken(payload) ?? JValue.CreateNull();
            var current = version;

            while (current < context.CurrentVersion)
            {
                if (!context.Migrators.TryGetValue(current, out var migrator))
                {
                    throw new SaveSystemException($"No migrator registered for version {current}.");
                }

                token = migrator.Migrate(token) ?? throw new SaveSystemException($"Migrator '{migrator.GetType().Name}' returned null token.");
                if (migrator.ToVersion <= current)
                {
                    throw new SaveSystemException($"Migrator '{migrator.GetType().Name}' does not advance the version.");
                }

                current = migrator.ToVersion;
            }

            if (current != context.CurrentVersion)
            {
                throw new SaveSystemException($"Migration ended at version {current}, expected {context.CurrentVersion}.");
            }

            return context.Serializer.SerializeFromToken(token);
        }

        private sealed class SaveEntry
        {
            /// <summary>
            /// Profile segment derived from the save file path.
            /// </summary>
            public string Profile { get; set; }
            /// <summary>
            /// Category segment derived from the save file path.
            /// </summary>
            public string Category { get; set; }
            /// <summary>
            /// Save key (file name without extension).
            /// </summary>
            public string Key { get; set; }
            /// <summary>
            /// Absolute path to the save file.
            /// </summary>
            public string FilePath { get; set; }
            /// <summary>
            /// Path relative to the storage root.
            /// </summary>
            public string RelativePath { get; set; }
            /// <summary>
            /// File size in bytes.
            /// </summary>
            public long FileSize { get; set; }
            /// <summary>
            /// Last write timestamp of the save file.
            /// </summary>
            public DateTime LastWriteTime { get; set; }
            /// <summary>
            /// Deserialized envelope containing the payload and metadata.
            /// </summary>
            public SaveEnvelope Envelope { get; set; }
            /// <summary>
            /// Exception captured while reading the envelope, if any.
            /// </summary>
            public Exception Error { get; set; }
            /// <summary>
            /// Exception captured while decoding the payload preview, if any.
            /// </summary>
            public Exception DecodeError { get; set; }
            /// <summary>
            /// JSON preview of the payload, if decoded successfully.
            /// </summary>
            public string DataPreview { get; set; }
        }

        private sealed class InspectionContext
        {
            /// <summary>
            /// Serializer used to read envelopes and payloads from disk.
            /// </summary>
            public ISerializer Serializer { get; set; }
            /// <summary>
            /// Processing pipeline used to reverse save-time processing.
            /// </summary>
            public IProcessingPipeline Pipeline { get; set; }
            /// <summary>
            /// Migrators applied to older payload versions.
            /// </summary>
            public IReadOnlyDictionary<int, IDataMigrator> Migrators { get; set; }
            /// <summary>
            /// Current save version expected by the configuration.
            /// </summary>
            public int CurrentVersion { get; set; }
        }
    }
}

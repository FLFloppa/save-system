using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
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
        private Button _refreshButton;
        private ScrollView _contentScroll;
        private Label _statusLabel;
        private ToolbarSearchField _searchField;

        private SaveServiceConfiguration _queuedConfiguration;
        private string _searchFilter = string.Empty;
        private readonly List<string> _warnings = new List<string>();

        /// <summary>
        /// Unity callback that builds the window's UI Toolkit layout.
        /// </summary>
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1f;

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 6
                }
            };

            _configurationField = new ObjectField("Configuration")
            {
                objectType = typeof(SaveServiceConfiguration),
                style =
                {
                    flexGrow = 1f,
                    minWidth = 180
                }
            };
            _configurationField.RegisterValueChangedCallback(_ => RefreshEntries());
            header.Add(_configurationField);

            _refreshButton = new Button(RefreshEntries)
            {
                text = "Refresh",
                style =
                {
                    marginLeft = 6
                }
            };
            header.Add(_refreshButton);

            _searchField = new ToolbarSearchField();
            _searchField.style.marginLeft = 6;
            _searchField.style.minWidth = 160;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue?.Trim() ?? string.Empty;
                RebuildContent(_cachedEntries);
            });
            header.Add(_searchField);

            root.Add(header);

            _statusLabel = new Label
            {
                style =
                {
                    unityTextAlign = TextAnchor.UpperLeft,
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 6
                }
            };
            root.Add(_statusLabel);

            _contentScroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1f,
                    flexShrink = 1f
                }
            };
            root.Add(_contentScroll);

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
            _contentScroll.Clear();
            SetStatus(string.Empty);

            var configuration = _configurationField.value as SaveServiceConfiguration;
            if (configuration == null)
            {
                SetStatus("Select a `SaveServiceConfiguration` to inspect saves.");
                return;
            }

            if (configuration.Serializer == null)
            {
                SetStatus("Configuration is missing a serializer asset.");
                return;
            }

            if (configuration.StorageProvider == null)
            {
                SetStatus("Configuration is missing a storage provider asset.");
                return;
            }

            ISerializer serializer;
            try
            {
                serializer = configuration.Serializer.Build();
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to build serializer: {ex.Message}");
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
                SetStatus($"Failed to build storage provider: {ex.Message}");
                return;
            }

            if (fileSystemProvider == null)
            {
                SetStatus("Save Observer currently supports `FileSystemStorageProvider` only.");
                return;
            }

            var rootPath = fileSystemProvider.RootPath;
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                SetStatus("Storage root path is empty.");
                return;
            }

            if (!Directory.Exists(rootPath))
            {
                SetStatus($"Storage directory does not exist yet: {rootPath}");
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
                SetStatus($"Failed to enumerate saves: {ex.Message}");
                return;
            }

            if (_cachedEntries.Count == 0)
            {
                SetStatus("No save files found in the storage directory.");
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
            _contentScroll.Clear();

            var filter = string.IsNullOrEmpty(_searchFilter)
                ? null
                : _searchFilter.ToLowerInvariant();

            var grouped = entries
                .Where(e => filter == null || e.Key.ToLowerInvariant().Contains(filter) || e.Profile.ToLowerInvariant().Contains(filter) || e.Category.ToLowerInvariant().Contains(filter))
                .GroupBy(e => e.Profile)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            if (!grouped.Any())
            {
                SetStatus(string.IsNullOrEmpty(_searchFilter) ? "No save files found in the storage directory." : "No saves match the current search filter.");
                return;
            }

            SetStatus(string.Empty);

            foreach (var profileGroup in grouped)
            {
                var profileFoldout = new Foldout
                {
                    text = $"Profile: {profileGroup.Key}",
                    value = false,
                    style =
                    {
                        marginBottom = 6
                    }
                };

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
                            marginLeft = 10,
                            marginBottom = 4
                        }
                    };

                    foreach (var entry in categoryGroup.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        categoryFoldout.Add(BuildEntryElement(entry));
                    }

                    profileFoldout.Add(categoryFoldout);
                }

                _contentScroll.Add(profileFoldout);
            }
        }

        /// <summary>
        /// Creates a card visual element for a specific save entry.
        /// </summary>
        /// <param name="entry">Save entry metadata.</param>
        /// <returns>UI element representing the entry.</returns>
        private VisualElement BuildEntryElement(SaveEntry entry)
        {
            var card = new VisualElement
            {
                style =
                {
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.3f),
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    marginBottom = 4,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.25f)
                }
            };

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = 4
                }
            };

            var titleLabel = new Label(entry.Key)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13
                }
            };
            header.Add(titleLabel);

            var infoLabel = new Label($"{EditorUtility.FormatBytes(entry.FileSize)} · {entry.LastWriteTime:G}")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };
            header.Add(infoLabel);

            card.Add(header);

            card.Add(new Label(entry.RelativePath)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Italic,
                    fontSize = 11,
                    color = new Color(0.8f, 0.8f, 0.8f, 0.8f),
                    marginBottom = 4
                }
            });

            if (entry.Error != null)
            {
                card.Add(new HelpBox(entry.Error.Message, HelpBoxMessageType.Error));
                card.Add(BuildFooter(entry));
                return card;
            }

            if (!string.IsNullOrWhiteSpace(entry.Envelope?.Readable))
            {
                card.Add(new Label(entry.Envelope.Readable)
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
                card.Add(new HelpBox($"Failed to decode payload: {entry.DecodeError.Message}", HelpBoxMessageType.Warning)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                        marginBottom = 4
                    }
                });
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
                card.Add(previewFoldout);
            }

            if (entry.Envelope?.Metadata != null)
            {
                if (ReadableUtility.TryGetReadableElement(entry.Envelope.Metadata, out var metadataElement))
                {
                    metadataElement.style.marginBottom = 4;
                    card.Add(metadataElement);
                }
                else if (ReadableUtility.TryGetReadableString(entry.Envelope.Metadata, out var metadataReadable))
                {
                    card.Add(new HelpBox(metadataReadable, HelpBoxMessageType.Info)
                    {
                        style =
                        {
                            whiteSpace = WhiteSpace.Normal,
                            marginBottom = 4
                        }
                    });
                }
                else
                {
                    card.Add(new Label(entry.Envelope.Metadata.ToString())
                    {
                        style =
                        {
                            whiteSpace = WhiteSpace.Normal,
                            marginBottom = 4
                        }
                    });
                }
            }

            card.Add(BuildFooter(entry));
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
        private void SetStatus(string message)
        {
            _statusLabel.text = message;
            _statusLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
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
            if (string.IsNullOrEmpty(_statusLabel.text))
            {
                SetStatus(warningText);
            }
            else
            {
                SetStatus(_statusLabel.text + "\n" + warningText);
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

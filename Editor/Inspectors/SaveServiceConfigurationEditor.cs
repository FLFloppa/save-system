using System;
using System.Collections.Generic;
using System.Linq;
using FLFloppa.EditorHelpers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="SaveServiceConfiguration"/> providing validation, previews, and quick actions.
    /// </summary>
    [CustomEditor(typeof(SaveServiceConfiguration))]
    public sealed class SaveServiceConfigurationEditor : UnityEditor.Editor
    {
        private IVisualElementScheduledItem _scheduledValidation;
        private InspectorUi.ExpandableLists.ListControl _validationList;

        /// <summary>
        /// Builds the inspector UI using UI Toolkit controls.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = InspectorUi.Layout.CreateRoot();

            root.Add(InspectorUi.Layout.CreateHeader(
                "FLFloppa Save Service Configuration",
                "Configure the serializer, storage provider, processing pipeline, and data migrators used to build the runtime save service."));

            root.Add(CreateCoreSection(so));
            root.Add(CreateMigratorsSection(so));
            root.Add(CreatePipelinePreview());
            root.Add(CreateStorageInfo());
            root.Add(CreateActionsCard());

            var validationCard = InspectorUi.Cards.Create(
                "Validation",
                out var validationContent,
                "Live status of the configuration health.");
            var validationList = InspectorUi.ExpandableLists.Create(
                "Current Findings",
                "Warnings and guidance refresh automatically.",
                expanded: true);
            validationContent.Add(validationList.Root);
            _validationList = validationList;
            root.Add(validationCard);

            RefreshValidation(validationList);
            _scheduledValidation = root.schedule.Execute(() => RefreshValidation(_validationList)).Every(500);

            return root;
        }

        /// <summary>
        /// Resumes the scheduled validation refresh to run promptly.
        /// </summary>
        private void ScheduleValidationRefresh()
        {
            if (_validationList != null)
            {
                RefreshValidation(_validationList);
            }
        }

        /// <summary>
        /// Creates the inspector header element.
        /// </summary>
        /// <returns>Header container.</returns>
        /// <summary>
        /// Creates the core configuration section (version, serializer, storage, pipeline).
        /// </summary>
        /// <param name="so">Serialized object for property binding.</param>
        /// <returns>Card containing core properties.</returns>
        private VisualElement CreateCoreSection(SerializedObject so)
        {
            var card = InspectorUi.Cards.Create(
                "Core Components",
                out var content,
                "Primary assets used by the save service.");

            content.Add(CreatePropertyField(so.FindProperty("_currentVersion"), "Current Version"));
            content.Add(CreatePropertyField(so.FindProperty("_serializer"), "Serializer"));

            var storageField = CreatePropertyField(so.FindProperty("_storageProvider"), "Storage Provider");
            storageField.RegisterValueChangeCallback(_ => ScheduleValidationRefresh());
            content.Add(storageField);

            content.Add(CreatePropertyField(so.FindProperty("_processingPipeline"), "Processing Pipeline"));

            return card;
        }

        /// <summary>
        /// Creates the data migrators foldout section.
        /// </summary>
        /// <param name="so">Serialized object for property binding.</param>
        /// <returns>Foldout containing migrator configuration.</returns>
        private VisualElement CreateMigratorsSection(SerializedObject so)
        {
            var card = InspectorUi.Cards.Create(
                "Data Migrators",
                out var content,
                "Define migration steps required when upgrading save versions.");

            var migratorsField = CreatePropertyField(so.FindProperty("_migrators"), "Migrators");
            content.Add(migratorsField);

            InspectorUi.Controls.AddHelpBox(
                content,
                "Migrators execute based on their `FromVersion`. Ensure each version step is covered.",
                HelpBoxMessageType.Info);

            var summaryList = InspectorUi.ExpandableLists.Create(
                "Resolved Migrator Chain",
                "Preview the runtime order and version transitions produced by the configured migrators.",
                expanded: false);
            content.Add(summaryList.Root);

            void RefreshMigratorSummary()
            {
                summaryList.ClearItems();

                try
                {
                    var config = (SaveServiceConfiguration)target;
                    var runtimeMigrators = config.Migrators?
                        .Where(m => m != null)
                        .Select(m => m.Build())
                        .OrderBy(m => m.FromVersion)
                        .ToList();

                    if (runtimeMigrators == null || runtimeMigrators.Count == 0)
                    {
                        summaryList.ShowEmptyState("No migrators configured.");
                        return;
                    }

                    foreach (var migrator in runtimeMigrators)
                    {
                        summaryList.AddItem($"{migrator.FromVersion} → {migrator.ToVersion} · {migrator.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    summaryList.ShowEmptyState($"Unable to preview migrators: {ex.Message}");
                }
            }

            content.RegisterCallback<GeometryChangedEvent>(_ => RefreshMigratorSummary());
            content.schedule.Execute(RefreshMigratorSummary).Every(500);

            return card;
        }

        /// <summary>
        /// Creates a foldout showing the runtime processing pipeline order.
        /// </summary>
        /// <returns>Foldout containing pipeline preview.</returns>
        private VisualElement CreatePipelinePreview()
        {
            var card = InspectorUi.Cards.Create(
                "Pipeline Preview",
                out var content,
                "Visualize the runtime processing modules applied to save data.");

            var moduleList = InspectorUi.ExpandableLists.Create(
                "Module Order",
                "Modules run sequentially on save and reversed on load.",
                expanded: true);
            content.Add(moduleList.Root);

            var statusBox = InspectorUi.Controls.AddHelpBox(content, string.Empty, HelpBoxMessageType.Info);

            void RefreshPreview()
            {
                moduleList.ClearItems();

                try
                {
                    var config = (SaveServiceConfiguration)target;
                    var pipelineConfig = config.ProcessingPipeline;

                    if (pipelineConfig == null)
                    {
                        statusBox.messageType = HelpBoxMessageType.Info;
                        statusBox.text = "Assign a processing pipeline asset to preview module order.";
                        moduleList.ShowEmptyState("No pipeline assigned.");
                        return;
                    }

                    var runtimePipeline = TryBuildPipeline(pipelineConfig, out var errorMessage);
                    if (runtimePipeline == null)
                    {
                        statusBox.messageType = HelpBoxMessageType.Warning;
                        statusBox.text = errorMessage;
                        moduleList.ShowEmptyState("Unable to build pipeline.");
                        return;
                    }

                    var modules = runtimePipeline.GetProcessingChain();
                    if (modules == null || modules.Count == 0)
                    {
                        statusBox.messageType = HelpBoxMessageType.Info;
                        statusBox.text = "No modules configured – data will be stored unprocessed.";
                        moduleList.ShowEmptyState("Pipeline currently empty.");
                        return;
                    }

                    statusBox.messageType = HelpBoxMessageType.Info;
                    statusBox.text = $"Configured {modules.Count} module(s).";

                    for (var index = 0; index < modules.Count; index++)
                    {
                        var module = modules[index];
                        moduleList.AddItem($"{index + 1}. {module.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    statusBox.messageType = HelpBoxMessageType.Error;
                    statusBox.text = $"Failed to preview pipeline: {ex.Message}";
                    moduleList.ShowEmptyState("Preview unavailable.");
                }
            }

            content.RegisterCallback<GeometryChangedEvent>(_ => RefreshPreview());
            content.schedule.Execute(RefreshPreview).Every(500);

            return card;
        }

        /// <summary>
        /// Attempts to build the processing pipeline and returns an error message if unsuccessful.
        /// </summary>
        /// <param name="pipelineConfig">Pipeline configuration asset.</param>
        /// <param name="error">Output error message when build fails.</param>
        /// <returns>Built pipeline or <c>null</c> on failure.</returns>
        private static IProcessingPipeline TryBuildPipeline(ProcessingPipelineConfiguration pipelineConfig, out string error)
        {
            try
            {
                error = string.Empty;
                return pipelineConfig.Build();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Creates the actions row containing validation/build buttons and shortcuts.
        /// </summary>
        /// <returns>Horizontal container with action buttons.</returns>
        private VisualElement CreateActionsCard()
        {
            var card = InspectorUi.Cards.Create(
                "Actions",
                out var content,
                "Validate and navigate to related tooling.");

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    marginTop = InspectorUi.Layout.ButtonSpacing
                }
            };

            row.Add(InspectorUi.Controls.CreateActionButton("Validate & Build Service", ExecuteBuild));

            row.Add(InspectorUi.Controls.CreateActionButton("Ping Pipeline Asset", () =>
            {
                var pipeline = ((SaveServiceConfiguration)target).ProcessingPipeline;
                if (pipeline != null)
                {
                    EditorGUIUtility.PingObject(pipeline);
                }
            }));

            row.Add(InspectorUi.Controls.CreateActionButton("Open Save Observer", () =>
            {
                SaveObserverWindow.ShowWindow((SaveServiceConfiguration)target);
            }));

            content.Add(row);

            return card;
        }

        /// <summary>
        /// Validates the configuration by building the runtime service and displaying dialog feedback.
        /// </summary>
        private void ExecuteBuild()
        {
            try
            {
                var service = ((SaveServiceConfiguration)target).Build();
                if (service != null)
                {
                    EditorUtility.DisplayDialog(
                        "Save Service Build",
                        "SaveService built successfully. Instance is not retained.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Save Service Build Failed",
                    ex.Message,
                    "Close");
            }
        }

        /// <summary>
        /// Creates the storage debug foldout used to display resolved storage paths.
        /// </summary>
        /// <returns>Foldout with storage information.</returns>
        private VisualElement CreateStorageInfo()
        {
            var card = InspectorUi.Cards.Create(
                "Storage Debug",
                out var content,
                "Inspect the resolved storage provider paths.");

            var infoLabel = InspectorUi.Controls.CreateSummaryLabel(string.Empty);
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            content.Add(infoLabel);

            void RefreshStorageInfo()
            {
                try
                {
                    var config = (SaveServiceConfiguration)target;
                    if (config.StorageProvider is FileSystemStorageProviderAsset fileSystemAsset)
                    {
                        if (fileSystemAsset.Build() is FileSystemStorageProvider provider)
                        {
                            infoLabel.text = $"Resolved Root Path: {provider.RootPath}";
                            return;
                        }
                    }

                    infoLabel.text = "Select a `FileSystemStorageProvider` asset to preview the resolved root path.";
                }
                catch (Exception ex)
                {
                    infoLabel.text = $"Unable to resolve storage root: {ex.Message}";
                }
            }

            content.RegisterCallback<GeometryChangedEvent>(_ => RefreshStorageInfo());
            content.schedule.Execute(RefreshStorageInfo).Every(500);

            return card;
        }

        /// <summary>
        /// Refreshes the validation container with the latest configuration warnings/errors.
        /// </summary>
        /// <param name="container">Validation container element.</param>
        private void RefreshValidation(InspectorUi.ExpandableLists.ListControl validationList)
        {
            if (validationList == null)
            {
                return;
            }

            validationList.ClearItems();

            var messages = new List<(string Text, HelpBoxMessageType Type)>();
            var config = (SaveServiceConfiguration)target;

            if (config.Serializer == null)
            {
                messages.Add(("Assign a serializer configuration before building the service.", HelpBoxMessageType.Error));
            }

            if (config.StorageProvider == null)
            {
                messages.Add(("Assign a storage provider configuration before building the service.", HelpBoxMessageType.Error));
            }

            if (config.ProcessingPipeline == null)
            {
                messages.Add(("Assign a processing pipeline configuration before building the service.", HelpBoxMessageType.Warning));
            }

            if (config.Migrators != null && config.Migrators.Count > 0)
            {
                AppendMigratorValidation(messages, config);
            }

            if (messages.Count == 0)
            {
                messages.Add(("Configuration looks good. Remember to run validation when assets change.", HelpBoxMessageType.Info));
            }

            foreach (var (text, type) in messages)
            {
                var label = type switch
                {
                    HelpBoxMessageType.Error => $"❌ {text}",
                    HelpBoxMessageType.Warning => $"⚠️ {text}",
                    HelpBoxMessageType.Info => $"ℹ️ {text}",
                    HelpBoxMessageType.None => text,
                    _ => text
                };

                validationList.AddItem(label);
            }
        }

        /// <summary>
        /// Performs additional migrator validation, ensuring version continuity.
        /// </summary>
        /// <param name="messages">Validation message list to populate.</param>
        /// <param name="config">Configuration being validated.</param>
        private static void AppendMigratorValidation(List<(string Text, HelpBoxMessageType Type)> messages, SaveServiceConfiguration config)
        {
            try
            {
                var migrators = config.Migrators
                    .Where(m => m != null)
                    .Select(m => m.Build())
                    .OrderBy(m => m.FromVersion)
                    .ToList();

                if (migrators.Count == 0)
                {
                    return;
                }

                for (var i = 0; i < migrators.Count - 1; i++)
                {
                    var expectedNext = migrators[i].ToVersion;
                    var next = migrators[i + 1].FromVersion;
                    if (expectedNext != next)
                    {
                        messages.Add((
                            $"Migration gap detected: {migrators[i].ToVersion} -> {next}. Ensure versions form a continuous chain.",
                            HelpBoxMessageType.Warning));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                messages.Add((
                    $"Unable to validate migrators: {ex.Message}",
                    HelpBoxMessageType.Warning));
            }
        }

        /// <summary>
        /// Creates and binds a UI Toolkit property field for the provided serialized property.
        /// </summary>
        /// <param name="property">Serialized property to display.</param>
        /// <param name="label">Display label.</param>
        /// <returns>Configured property field.</returns>
        private PropertyField CreatePropertyField(SerializedProperty property, string label)
        {
            return property != null
                ? InspectorUi.Controls.CreatePropertyField(property, label)
                : new PropertyField { label = label };
        }
    }
}

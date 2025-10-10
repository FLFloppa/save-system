using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string ValidationContainerName = "SaveServiceConfigurationEditor_Validation";

        private IVisualElementScheduledItem _scheduledValidation;
        private VisualElement _latestValidationContainer;

        /// <summary>
        /// Builds the inspector UI using UI Toolkit controls.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            var root = new ScrollView
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                }
            };

            root.Add(BuildHeader());
            root.Add(CreateCoreSection(so));
            root.Add(CreateMigratorsSection(so));
            root.Add(CreatePipelinePreview());
            root.Add(CreateStorageInfo());
            root.Add(CreateActionsRow());

            var validation = new VisualElement { name = ValidationContainerName };
            root.Add(validation);

            RefreshValidation(validation);
            _scheduledValidation = root.schedule.Execute(() => RefreshValidation(validation)).Every(500);

            return root;
        }

        /// <summary>
        /// Resumes the scheduled validation refresh to run promptly.
        /// </summary>
        private void ScheduleValidationRefresh()
        {
            _scheduledValidation?.Pause();
            _scheduledValidation?.Resume();
        }

        /// <summary>
        /// Creates the inspector header element.
        /// </summary>
        /// <returns>Header container.</returns>
        private static VisualElement BuildHeader()
        {
            var container = new VisualElement();
            container.Add(new Label("FLFloppa Save Service Configuration")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    marginBottom = 4
                }
            });

            container.Add(new Label("Configure the serializer, storage provider, processing pipeline, and data migrators used to build the runtime save service."));

            var spacer = new VisualElement();
            spacer.style.marginBottom = 6;
            container.Add(spacer);

            return container;
        }

        /// <summary>
        /// Creates the core configuration section (version, serializer, storage, pipeline).
        /// </summary>
        /// <param name="so">Serialized object for property binding.</param>
        /// <returns>Foldout containing core properties.</returns>
        private VisualElement CreateCoreSection(SerializedObject so)
        {
            var foldout = new Foldout { text = "Core Components", value = true };

            foldout.Add(CreatePropertyField(so.FindProperty("_currentVersion"), "Current Version"));
            foldout.Add(CreatePropertyField(so.FindProperty("_serializer"), "Serializer"));
            var storageField = CreatePropertyField(so.FindProperty("_storageProvider"), "Storage Provider");
            storageField.RegisterValueChangeCallback(_ => ScheduleValidationRefresh());
            foldout.Add(storageField);
            foldout.Add(CreatePropertyField(so.FindProperty("_processingPipeline"), "Processing Pipeline"));

            return foldout;
        }

        /// <summary>
        /// Creates the data migrators foldout section.
        /// </summary>
        /// <param name="so">Serialized object for property binding.</param>
        /// <returns>Foldout containing migrator configuration.</returns>
        private VisualElement CreateMigratorsSection(SerializedObject so)
        {
            var foldout = new Foldout { text = "Data Migrators", value = true };
            var migrators = CreatePropertyField(so.FindProperty("_migrators"), "Migrators");
            foldout.Add(migrators);

            var hint = new HelpBox(
                "Migrators are executed based on their FromVersion. Ensure each version step is covered.",
                HelpBoxMessageType.Info)
            {
                style = { marginTop = 4 }
            };

            foldout.Add(hint);
            return foldout;
        }

        /// <summary>
        /// Creates a foldout showing the runtime processing pipeline order.
        /// </summary>
        /// <returns>Foldout containing pipeline preview.</returns>
        private VisualElement CreatePipelinePreview()
        {
            var container = new Foldout { text = "Pipeline Preview", value = true };
            var list = new IMGUIContainer(() =>
            {
                try
                {
                    var config = (SaveServiceConfiguration)target;
                    var pipeline = config.ProcessingPipeline;

                    if (pipeline == null)
                    {
                        EditorGUILayout.HelpBox("Assign a processing pipeline configuration to preview the module order.", MessageType.Info);
                        return;
                    }

                    EditorGUILayout.LabelField("Module Order", EditorStyles.boldLabel);

                    var modulesProp = serializedObject.FindProperty("_processingPipeline");
                    if (modulesProp == null || modulesProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.LabelField("(none)");
                        return;
                    }

                    if (config.ProcessingPipeline is AllProcessingPipelineAsset pipelineAsset)
                    {
                        // Nothing to draw in IMGUI; UI Toolkit field handles editing. Provide doc text.
                    }

                    var runtimePipeline = TryBuildPipeline(pipeline, out var errorMessage);
                    if (runtimePipeline == null)
                    {
                        EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
                        return;
                    }

                    var modules = runtimePipeline.GetProcessingChain();
                    if (modules == null || modules.Count == 0)
                    {
                        EditorGUILayout.LabelField("No modules configured – data will be stored unprocessed.");
                    }
                    else
                    {
                        for (var index = 0; index < modules.Count; index++)
                        {
                            var module = modules[index];
                            EditorGUILayout.LabelField($"{index + 1}. {module.GetType().Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    EditorGUILayout.HelpBox($"Failed to preview pipeline: {ex.Message}", MessageType.Error);
                }
            })
            {
                style = { marginTop = 4, marginBottom = 4 }
            };

            container.Add(list);
            return container;
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
        private VisualElement CreateActionsRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    marginBottom = 6
                }
            };

            var buildButton = new Button(() => ExecuteBuild())
            {
                text = "Validate & Build Service"
            };
            buildButton.style.marginRight = 6;

            row.Add(buildButton);

            var pingButton = new Button(() =>
            {
                var pipeline = ((SaveServiceConfiguration)target).ProcessingPipeline;
                if (pipeline != null)
                {
                    EditorGUIUtility.PingObject(pipeline);
                }
            })
            {
                text = "Ping Pipeline Asset"
            };
            row.Add(pingButton);

            var observerButton = new Button(() => SaveObserverWindow.ShowWindow((SaveServiceConfiguration)target))
            {
                text = "Open Save Observer"
            };
            observerButton.style.marginLeft = 6;
            row.Add(observerButton);

            return row;
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
            var foldout = new Foldout { text = "Storage Debug", value = false };
            var infoLabel = new Label { style = { whiteSpace = WhiteSpace.Normal } };

            foldout.Add(infoLabel);

            foldout.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                try
                {
                    var config = (SaveServiceConfiguration)target;
                    if (config.StorageProvider is FileSystemStorageProviderAsset fileSystemAsset)
                    {
                        var provider = fileSystemAsset.Build() as FileSystemStorageProvider;
                        if (provider != null)
                        {
                            infoLabel.text = $"Resolved Root Path: {provider.RootPath}";
                            return;
                        }
                    }

                    infoLabel.text = "Select FileSystemStorageProvider asset to preview root path.";
                }
                catch (Exception ex)
                {
                    infoLabel.text = $"Unable to resolve storage root: {ex.Message}";
                }
            });

            return foldout;
        }

        /// <summary>
        /// Refreshes the validation container with the latest configuration warnings/errors.
        /// </summary>
        /// <param name="container">Validation container element.</param>
        private void RefreshValidation(VisualElement container)
        {
            _latestValidationContainer = container;
            container.Clear();

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
                container.Add(new HelpBox(text, type)
                {
                    style = { marginTop = 2, marginBottom = 2 }
                });
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
            if (property == null)
            {
                return new PropertyField { label = label };
            }

            var field = new PropertyField(property, label);
            field.Bind(property.serializedObject);
            return field;
        }
    }
}

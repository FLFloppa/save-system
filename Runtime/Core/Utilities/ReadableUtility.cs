using System;
using System.Linq;
using System.Reflection;
using System.Text;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Helper utilities for producing human-readable representations of save data and metadata.
    /// </summary>
    public static class ReadableUtility
    {
        private const BindingFlags MethodSearchFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Builds a readable summary string for the supplied save payload and metadata.
        /// </summary>
        /// <param name="data">Payload object to summarize.</param>
        /// <param name="metadata">Metadata associated with the payload.</param>
        /// <returns>A combined summary string or <c>null</c> if no readable text is available.</returns>
        public static string CreateReadableSummary(object data, IMetadata metadata)
        {
            var builder = new StringBuilder();

            AppendReadable(builder, data, "Data");
            AppendReadable(builder, metadata, "Metadata");

            var result = builder.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        /// <summary>
        /// Attempts to obtain a readable string for the supplied object.
        /// </summary>
        /// <param name="target">Object to inspect.</param>
        /// <param name="readable">Readable string, if available.</param>
        /// <returns><c>true</c> if a string was produced; otherwise, <c>false</c>.</returns>
        public static bool TryGetReadableString(object target, out string readable)
        {
            readable = null;
            if (target == null)
            {
                return false;
            }

            try
            {
                if (target is ISaveReadable readableProvider)
                {
                    readable = readableProvider.ToReadableString();
                    return !string.IsNullOrWhiteSpace(readable);
                }

                var method = target.GetType()
                    .GetMethods(MethodSearchFlags)
                    .FirstOrDefault(m => m.GetCustomAttribute<SaveReadableMethodAttribute>() != null);

                if (method != null && method.ReturnType == typeof(string) && method.GetParameters().Length == 0)
                {
                    readable = method.Invoke(target, null) as string;
                    return !string.IsNullOrWhiteSpace(readable);
                }
            }
            catch (Exception)
            {
                // Swallow reflection errors – readable output is non-critical.
            }

            readable = null;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Attempts to obtain a custom UI Toolkit element describing the supplied object.
        /// </summary>
        /// <param name="target">Object to inspect.</param>
        /// <param name="element">Resulting visual element if successful.</param>
        /// <returns><c>true</c> if a visual element was produced; otherwise, <c>false</c>.</returns>
        public static bool TryGetReadableElement(object target, out VisualElement element)
        {
            element = null;
            if (target == null)
            {
                return false;
            }

            try
            {
                if (target is ISaveReadableElement elementProvider)
                {
                    element = elementProvider.CreateReadableElement();
                    return element != null;
                }

                var method = target.GetType()
                    .GetMethods(MethodSearchFlags)
                    .FirstOrDefault(m => m.GetCustomAttribute<SaveReadableElementMethodAttribute>() != null);

                if (method != null && typeof(VisualElement).IsAssignableFrom(method.ReturnType) && method.GetParameters().Length == 0)
                {
                    element = method.Invoke(target, null) as VisualElement;
                    return element != null;
                }
            }
            catch (Exception)
            {
                // UI helpers are optional – ignore reflection or invocation errors.
            }

            element = null;
            return false;
        }
#endif

        /// <summary>
        /// Appends a readable representation for the specified target into the builder.
        /// </summary>
        /// <param name="builder">Destination string builder.</param>
        /// <param name="target">Object to summarize.</param>
        /// <param name="label">Fallback label used when no readable string is available.</param>
        private static void AppendReadable(StringBuilder builder, object target, string label)
        {
            if (target == null)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            if (TryGetReadableString(target, out var readable))
            {
                builder.Append(readable);
            }
            else
            {
                builder.Append(label).Append(": ").Append(target);
            }
        }
    }
}

using System;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Implement on save data or metadata types to provide a friendly string representation for tooling.
    /// </summary>
    public interface ISaveReadable
    {
        /// <summary>
        /// Returns a human-readable string describing the contents of the object.
        /// </summary>
        string ToReadableString();
    }

    /// <summary>
    /// Marks an instance method that returns a readable representation for save inspection tools.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SaveReadableMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// Provides runtime annotations for readable UI via attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SaveReadableElementMethodAttribute : Attribute
    {
    }

#if UNITY_EDITOR
    /// <summary>
    /// Implement on save data or metadata types to supply custom UI Toolkit visualisations in editor tooling.
    /// </summary>
    public interface ISaveReadableElement
    {
        /// <summary>
        /// Creates a <see cref="VisualElement"/> describing the object. Called on demand by editor tools.
        /// </summary>
        VisualElement CreateReadableElement();
    }
#endif
}

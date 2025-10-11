#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace FLFloppa.SaveSystem
{
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
}
#endif
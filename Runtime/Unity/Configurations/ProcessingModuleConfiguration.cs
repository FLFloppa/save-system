using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Base ScriptableObject for creating processing module instances used within a pipeline.
    /// </summary>
    public abstract class ProcessingModuleConfiguration : ScriptableObject
    {
        /// <summary>
        /// Creates the runtime <see cref="IProcessingModule"/> implementation.
        /// </summary>
        /// <returns>Configured processing module instance.</returns>
        public abstract IProcessingModule Build();
    }
}
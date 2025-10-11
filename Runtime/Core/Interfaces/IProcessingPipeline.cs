using System.Collections.Generic;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Orchestrates a sequence of <see cref="IProcessingModule"/> instances for save/load flows.
    /// </summary>
    public interface IProcessingPipeline
    {
        /// <summary>
        /// Gets the configured modules in the pipeline.
        /// </summary>
        IReadOnlyList<IProcessingModule> AllModules { get; }

        /// <summary>
        /// Applies forward processing for save operations.
        /// </summary>
        /// <param name="data">Payload bytes to transform.</param>
        /// <returns>Processed bytes suitable for persistence.</returns>
        byte[] ProcessSave(byte[] data);

        /// <summary>
        /// Applies reverse processing during load operations.
        /// </summary>
        /// <param name="data">Stored bytes to restore.</param>
        /// <returns>Restored payload bytes.</returns>
        byte[] ProcessLoad(byte[] data);

        /// <summary>
        /// Returns the effective processing order for debugging or visualization.
        /// </summary>
        /// <returns>A snapshot of the processing chain.</returns>
        IReadOnlyList<IProcessingModule> GetProcessingChain();
    }
}
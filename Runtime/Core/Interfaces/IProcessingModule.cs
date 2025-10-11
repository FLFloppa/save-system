namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Represents a reversible processing step applied to save payloads (e.g., encryption, compression).
    /// </summary>
    public interface IProcessingModule
    {
        /// <summary>
        /// Processes bytes prior to persistence.
        /// </summary>
        /// <param name="input">The payload to transform.</param>
        /// <returns>The processed bytes.</returns>
        byte[] Process(byte[] input);

        /// <summary>
        /// Reverses <see cref="Process"/> during load.
        /// </summary>
        /// <param name="input">The processed bytes.</param>
        /// <returns>The restored payload bytes.</returns>
        byte[] Reverse(byte[] input);
    }
}
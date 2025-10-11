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
}
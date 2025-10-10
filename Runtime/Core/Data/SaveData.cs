namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Wrapper combining loaded data with associated metadata.
    /// </summary>
    /// <typeparam name="T">Type of the deserialized data object.</typeparam>
    public sealed class SaveData<T>
    {
        /// <summary>
        /// Gets the deserialized payload associated with the save entry.
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// Gets metadata captured when the entry was saved.
        /// </summary>
        public IMetadata Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveData{T}"/> class.
        /// </summary>
        /// <param name="data">The deserialized payload.</param>
        /// <param name="metadata">Metadata bundled with the payload.</param>
        public SaveData(T data, IMetadata metadata)
        {
            Data = data;
            Metadata = metadata;
        }
    }
}

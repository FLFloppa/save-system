namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Container holding version, metadata, and payload bytes for persistence.
    /// </summary>
    public sealed class SaveEnvelope
    {
        /// <summary>
        /// Schema version of the serialized payload.
        /// </summary>
        public int Version;

        /// <summary>
        /// Optional metadata captured alongside the payload.
        /// </summary>
        public IMetadata Metadata;

        /// <summary>
        /// Processed payload bytes ready for persistence.
        /// </summary>
        public byte[] Payload;

        /// <summary>
        /// Human-friendly summary produced during save operations.
        /// </summary>
        public string Readable;
    }
}

using System;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Standard metadata captured for each save.
    /// </summary>
    [Serializable]
    public class StandardMetadata : IMetadata, ISaveReadable
    {
        /// <summary>
        /// Gets or sets the timestamp recorded when the save occurred (UTC recommended).
        /// </summary>
        public DateTime SaveTime { get; set; }

        /// <summary>
        /// Gets or sets the version string of the game/client that produced the save.
        /// </summary>
        public string GameVersion { get; set; }

        /// <summary>
        /// Produces a concise textual representation of the metadata contents.
        /// </summary>
        /// <returns>A human-readable summary.</returns>
        public virtual string ToReadableString()
        {
            return $"Saved {SaveTime:G} (Game {GameVersion ?? "unknown"})";
        }
    }
}
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Base ScriptableObject for creating serializer instances used by the save system.
    /// </summary>
    public abstract class SerializerConfiguration : ScriptableObject
    {
        /// <summary>
        /// Creates the runtime <see cref="ISerializer"/> implementation.
        /// </summary>
        /// <returns>Configured serializer instance.</returns>
        public abstract ISerializer Build();
    }
}
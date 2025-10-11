using Newtonsoft.Json.Linq;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Defines serialization services for save payloads and envelopes.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes a CLR object graph into a byte array.
        /// </summary>
        /// <param name="data">The instance to serialize.</param>
        /// <returns>Serialized bytes representing <paramref name="data"/>.</returns>
        byte[] Serialize(object data);

        /// <summary>
        /// Deserializes bytes into an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The destination type.</typeparam>
        /// <param name="data">The serialized payload.</param>
        /// <returns>The reconstructed object.</returns>
        T Deserialize<T>(byte[] data);

        /// <summary>
        /// Deserializes bytes into a JSON token for migration or inspection purposes.
        /// </summary>
        /// <param name="data">The serialized payload.</param>
        /// <returns>The JSON token representation.</returns>
        JToken DeserializeToToken(byte[] data);

        /// <summary>
        /// Serializes a JSON token back into a byte array.
        /// </summary>
        /// <param name="token">The token to serialize.</param>
        /// <returns>Serialized representation of <paramref name="token"/>.</returns>
        byte[] SerializeFromToken(JToken token);
    }
}
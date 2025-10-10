using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Newtonsoft.Json-based serializer implementation for the save system.
    /// </summary>
    public sealed class JsonNetSerializer : ISerializer
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetSerializer"/> class using the provided settings.
        /// </summary>
        /// <param name="settings">Optional Json.NET settings; if <c>null</c>, defaults are used.</param>
        public JsonNetSerializer(JsonSerializerSettings settings)
        {
            _serializer = JsonSerializer.Create(settings ?? new JsonSerializerSettings());
        }

        /// <inheritdoc />
        public byte[] Serialize(object data)
        {
            if (data == null)
            {
                throw new SaveSystemException("Cannot serialize a null object.");
            }

            try
            {
                var json = SerializeToString(data);
                return Utf8NoBom.GetBytes(json);
            }
            catch (JsonException ex)
            {
                throw new SaveSystemException("Failed to serialize object with Json.NET.", ex);
            }
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            var json = DecodeToString(data);

            try
            {
                using var stringReader = new StringReader(json);
                using var jsonReader = new JsonTextReader(stringReader);
                var result = _serializer.Deserialize<T>(jsonReader);

                if (result == null)
                {
                    throw new SaveSystemException("Deserialized object is null.");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new SaveSystemException($"Failed to deserialize JSON into {typeof(T).Name}.", ex);
            }
        }

        /// <inheritdoc />
        public JToken DeserializeToToken(byte[] data)
        {
            var json = DecodeToString(data);

            try
            {
                return JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new SaveSystemException("Failed to parse JSON payload into JToken.", ex);
            }
        }

        /// <inheritdoc />
        public byte[] SerializeFromToken(JToken token)
        {
            if (token == null)
            {
                throw new SaveSystemException("Cannot serialize a null JToken.");
            }

            try
            {
                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var jsonWriter = new JsonTextWriter(stringWriter);
                token.WriteTo(jsonWriter);
                jsonWriter.Flush();
                return Utf8NoBom.GetBytes(stringWriter.ToString());
            }
            catch (JsonException ex)
            {
                throw new SaveSystemException("Failed to serialize JToken to JSON.", ex);
            }
        }

        /// <summary>
        /// Serializes a CLR object to a JSON string using the configured serializer.
        /// </summary>
        /// <param name="data">Object to serialize.</param>
        /// <returns>JSON string.</returns>
        private string SerializeToString(object data)
        {
            using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            using var jsonWriter = new JsonTextWriter(stringWriter);
            _serializer.Serialize(jsonWriter, data);
            jsonWriter.Flush();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Decodes UTF-8 bytes into a JSON string, validating input.
        /// </summary>
        /// <param name="data">Encoded JSON bytes.</param>
        /// <returns>Decoded JSON string.</returns>
        private string DecodeToString(byte[] data)
        {
            if (data == null)
            {
                throw new SaveSystemException("Cannot deserialize from a null byte array.");
            }

            try
            {
                return Utf8NoBom.GetString(data);
            }
            catch (DecoderFallbackException ex)
            {
                throw new SaveSystemException("Provided byte array is not valid UTF-8 JSON.", ex);
            }
        }
    }
}

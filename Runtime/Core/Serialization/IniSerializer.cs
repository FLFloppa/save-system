using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Serializer that emits human-readable INI-style text documents.
    /// </summary>
    public sealed class IniSerializer : ISerializer
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);
        private readonly JsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="IniSerializer"/> class.
        /// </summary>
        /// <param name="settings">Optional Json.NET settings used when converting between CLR objects and JSON tokens.</param>
        public IniSerializer(JsonSerializerSettings settings = null)
        {
            _jsonSerializer = JsonSerializer.Create(settings ?? new JsonSerializerSettings());
        }

        /// <inheritdoc />
        public byte[] Serialize(object data)
        {
            if (data == null)
            {
                throw new SaveSystemException("Cannot serialize a null object with IniSerializer.");
            }

            var token = JToken.FromObject(data, _jsonSerializer);
            var text = SerializeFromToken(token);
            return text;
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data)
        {
            var token = DeserializeToToken(data);
            try
            {
                return token.ToObject<T>(_jsonSerializer);
            }
            catch (JsonException ex)
            {
                throw new SaveSystemException($"Failed to materialize type {typeof(T).Name} from INI data.", ex);
            }
        }

        /// <inheritdoc />
        public JToken DeserializeToToken(byte[] data)
        {
            if (data == null)
            {
                throw new SaveSystemException("Cannot deserialize null INI data.");
            }

            var text = Utf8NoBom.GetString(data);
            return Parse(text);
        }

        /// <inheritdoc />
        public byte[] SerializeFromToken(JToken token)
        {
            if (token == null)
            {
                throw new SaveSystemException("Cannot serialize a null token with IniSerializer.");
            }

            var iniText = BuildIni(token);
            return Utf8NoBom.GetBytes(iniText);
        }

        /// <summary>
        /// Translates a JSON token into an INI document string by flattening nested structures into sections and keys.
        /// </summary>
        /// <param name="token">Token to serialize.</param>
        /// <returns>INI-formatted text.</returns>
        private static string BuildIni(JToken token)
        {
            var sections = new Dictionary<string, List<(string Key, JToken Value)>>(StringComparer.Ordinal);

            void AddLeaf(string path, JToken value)
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = "value";
                }

                var split = path.LastIndexOf('/');
                var section = split >= 0 ? path.Substring(0, split) : string.Empty;
                var key = split >= 0 ? path.Substring(split + 1) : path;

                if (!sections.TryGetValue(section, out var list))
                {
                    list = new List<(string, JToken)>();
                    sections.Add(section, list);
                }

                list.Add((key, value));
            }

            void Traverse(JToken current, string path)
            {
                switch (current.Type)
                {
                    case JTokenType.Object:
                        var obj = (JObject)current;
                        if (!obj.HasValues)
                        {
                            AddLeaf(path, current);
                            return;
                        }

                        foreach (var property in obj.Properties())
                        {
                            var next = string.IsNullOrEmpty(path)
                                ? property.Name
                                : string.Concat(path, "/", property.Name);
                            Traverse(property.Value, next);
                        }
                        break;
                    case JTokenType.Array:
                        var array = (JArray)current;
                        if (array.Count == 0)
                        {
                            AddLeaf(path, current);
                            return;
                        }

                        for (var i = 0; i < array.Count; i++)
                        {
                            var next = string.IsNullOrEmpty(path)
                                ? i.ToString(CultureInfo.InvariantCulture)
                                : string.Concat(path, "/", i.ToString(CultureInfo.InvariantCulture));
                            Traverse(array[i], next);
                        }
                        break;
                    default:
                        AddLeaf(path, current);
                        break;
                }
            }

            Traverse(token, string.Empty);

            var builder = new StringBuilder();
            foreach (var section in sections.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (!string.IsNullOrEmpty(section.Key))
                {
                    builder.Append('[').Append(section.Key).AppendLine("]");
                }

                foreach (var entry in section.Value)
                {
                    builder.Append(entry.Key)
                        .Append(" = ")
                        .AppendLine(FormatValue(entry.Value));
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Formats a JSON token into an INI-friendly scalar value.
        /// </summary>
        /// <param name="token">Token to stringify.</param>
        /// <returns>INI scalar representation.</returns>
        private static string FormatValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Null => "null",
                JTokenType.Boolean => token.Value<bool>() ? "true" : "false",
                JTokenType.Integer => token.Value<long>().ToString(CultureInfo.InvariantCulture),
                JTokenType.Float => token.Value<double>().ToString(CultureInfo.InvariantCulture),
                JTokenType.Date => token.Value<DateTime>().ToString("o", CultureInfo.InvariantCulture),
                JTokenType.Bytes => Convert.ToBase64String(token.Value<byte[]>()),
                JTokenType.String => Quote(token.Value<string>() ?? string.Empty),
                JTokenType.Guid => token.Value<Guid>().ToString("D"),
                JTokenType.Object or JTokenType.Array => Quote(token.ToString(Formatting.None)),
                _ => Quote(token.ToString())
            };
        }

        /// <summary>
        /// Escapes a string literal for inclusion in an INI file.
        /// </summary>
        /// <param name="value">Raw string.</param>
        /// <returns>Escaped and quoted string.</returns>
        private static string Quote(string value)
        {
            var escaped = value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
            return string.Concat('\"', escaped, '\"');
        }

        /// <summary>
        /// Parses INI text back into a JSON token structure.
        /// </summary>
        /// <param name="text">INI content.</param>
        /// <returns>JSON token representing the data.</returns>
        private JToken Parse(string text)
        {
            var reader = new StringReader(text ?? string.Empty);
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            string currentSection = string.Empty;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                var equalIndex = line.IndexOf('=');
                if (equalIndex < 0)
                {
                    continue;
                }

                var key = line.Substring(0, equalIndex).Trim();
                var value = line.Substring(equalIndex + 1).Trim();
                var path = string.IsNullOrEmpty(currentSection) ? key : string.Concat(currentSection, "/", key);
                entries[path] = value;
            }

            if (entries.Count == 0)
            {
                return JValue.CreateNull();
            }

            var root = new JObject();
            foreach (var kvp in entries)
            {
                var segments = kvp.Key.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                InsertValue(root, segments, ParseValue(kvp.Value));
            }

            return root.HasValues ? root : JValue.CreateNull();
        }

        /// <summary>
        /// Converts a raw INI value string into an appropriate JSON token.
        /// </summary>
        /// <param name="raw">Raw INI value.</param>
        /// <returns>Parsed JSON token.</returns>
        private static JToken ParseValue(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return JValue.CreateNull();
            }

            if (string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase))
            {
                return JValue.CreateNull();
            }

            if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            {
                return new JValue(Unquote(raw));
            }

            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            {
                return new JValue(longValue);
            }

            if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
            {
                return new JValue(doubleValue);
            }

            if (bool.TryParse(raw, out var boolValue))
            {
                return new JValue(boolValue);
            }

            if (DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                return new JValue(dateTime);
            }

            try
            {
                var asJson = JToken.Parse(UnquoteIfQuoted(raw));
                return asJson;
            }
            catch
            {
                // fall through
            }

            try
            {
                return new JValue(Convert.FromBase64String(raw));
            }
            catch
            {
                // ignore
            }

            return new JValue(UnquoteIfQuoted(raw));
        }

        /// <summary>
        /// Removes surrounding quotes and unescapes escape sequences from a string.
        /// </summary>
        /// <param name="value">Quoted string.</param>
        /// <returns>Unescaped string value.</returns>
        private static string Unquote(string value)
        {
            var inner = value.Substring(1, value.Length - 2);
            var builder = new StringBuilder(inner.Length);
            for (var i = 0; i < inner.Length; i++)
            {
                var c = inner[i];
                if (c == '\\' && i + 1 < inner.Length)
                {
                    var next = inner[++i];
                    builder.Append(next switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '"' => '"',
                        '\\' => '\\',
                        _ => next
                    });
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Returns the unquoted string if the value is enclosed in quotes; otherwise returns the value as-is.
        /// </summary>
        /// <param name="value">String to inspect.</param>
        /// <returns>Unquoted or original string.</returns>
        private static string UnquoteIfQuoted(string value)
        {
            return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
                ? Unquote(value)
                : value;
        }

        /// <summary>
        /// Inserts a value into the JSON hierarchy according to the provided path segments, creating objects/arrays as needed.
        /// </summary>
        /// <param name="root">Root object to mutate.</param>
        /// <param name="segments">Path segments (object keys or array indices).</param>
        /// <param name="value">Value to insert.</param>
        private static void InsertValue(JObject root, IReadOnlyList<string> segments, JToken value)
        {
            if (segments.Count == 0)
            {
                root["value"] = value;
                return;
            }

            JToken current = root;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var isLast = i == segments.Count - 1;
                var isIndex = int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index);

                if (isLast)
                {
                    if (isIndex)
                    {
                        var array = current as JArray ?? throw new SaveSystemException("Numeric path segment requires array parent in INI data.");
                        EnsureArraySize(array, index + 1);
                        array[index] = value;
                    }
                    else
                    {
                        if (current is JObject obj)
                        {
                            obj[segment] = value;
                        }
                        else if (current is JArray array)
                        {
                            array.Add(value);
                        }
                    }
                }
                else
                {
                    var nextSegment = segments[i + 1];
                    var nextIsIndex = int.TryParse(nextSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

                    if (isIndex)
                    {
                        var parentArray = current as JArray ?? throw new SaveSystemException("Numeric path segment requires array parent in INI data.");
                        EnsureArraySize(parentArray, index + 1);
                        var child = parentArray[index];
                        if (child == null || child.Type == JTokenType.Null)
                        {
                            child = nextIsIndex ? new JArray() : new JObject();
                            parentArray[index] = child;
                        }
                        current = child;
                    }
                    else
                    {
                        if (current is JObject obj)
                        {
                            var child = obj[segment];
                            if (child == null || child.Type == JTokenType.Null)
                            {
                                child = nextIsIndex ? (JToken)new JArray() : new JObject();
                                obj[segment] = child;
                            }
                            current = child;
                        }
                        else if (current is JArray arr)
                        {
                            var child = new JObject();
                            arr.Add(child);
                            current = child;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ensures the specified JSON array has at least the desired size, expanding with nulls as needed.
        /// </summary>
        /// <param name="array">Array to grow.</param>
        /// <param name="size">Required minimum length.</param>
        private static void EnsureArraySize(JArray array, int size)
        {
            while (array.Count < size)
            {
                array.Add(JValue.CreateNull());
            }
        }
    }
}

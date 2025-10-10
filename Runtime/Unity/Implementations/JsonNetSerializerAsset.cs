using Newtonsoft.Json;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "JsonNetSerializer",
        menuName = "FLFloppa/Save System/Serializer/Newtonsoft Json",
        order = 10)]
    public sealed class JsonNetSerializerAsset : SerializerConfiguration
    {
        [SerializeField]
        [Tooltip("Controls how type information is included in the JSON output.")]
        private TypeNameHandling _typeNameHandling = TypeNameHandling.Auto;

        [SerializeField]
        [Tooltip("Controls how null values are handled during serialization.")]
        private NullValueHandling _nullValueHandling = NullValueHandling.Ignore;

        [SerializeField]
        [Tooltip("Controls how missing members are handled during deserialization.")]
        private MissingMemberHandling _missingMemberHandling = MissingMemberHandling.Ignore;

        [SerializeField]
        [Tooltip("Controls how default values are handled during serialization.")]
        private DefaultValueHandling _defaultValueHandling = DefaultValueHandling.Ignore;

        [SerializeField]
        [Tooltip("Controls if output JSON should be indented for readability.")]
        private Formatting _formatting = Formatting.None;

        public override ISerializer Build()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = _typeNameHandling,
                NullValueHandling = _nullValueHandling,
                MissingMemberHandling = _missingMemberHandling,
                DefaultValueHandling = _defaultValueHandling,
                Formatting = _formatting
            };

            return new JsonNetSerializer(settings);
        }
    }
}

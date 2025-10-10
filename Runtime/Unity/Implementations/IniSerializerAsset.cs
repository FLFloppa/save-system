using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "IniSerializer",
        menuName = "FLFloppa/Save System/Serializer/INI",
        order = 20)]
    public sealed class IniSerializerAsset : SerializerConfiguration
    {
        [SerializeField]
        [Tooltip("Controls how type metadata is emitted. Auto is recommended when saving polymorphic data.")]
        private TypeNameHandling _typeNameHandling = TypeNameHandling.Auto;

        [SerializeField]
        [Tooltip("Controls how null values are treated during serialization.")]
        private NullValueHandling _nullValueHandling = NullValueHandling.Ignore;

        [SerializeField]
        [Tooltip("Controls how default values are treated during serialization.")]
        private DefaultValueHandling _defaultValueHandling = DefaultValueHandling.Ignore;

        [SerializeField]
        [Tooltip("Controls how missing members are handled during deserialization.")]
        private MissingMemberHandling _missingMemberHandling = MissingMemberHandling.Ignore;

        public override ISerializer Build()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = _typeNameHandling,
                NullValueHandling = _nullValueHandling,
                DefaultValueHandling = _defaultValueHandling,
                MissingMemberHandling = _missingMemberHandling
            };

            return new IniSerializer(settings);
        }
    }
}

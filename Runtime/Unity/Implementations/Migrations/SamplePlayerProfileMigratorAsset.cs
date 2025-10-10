using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "SamplePlayerProfileMigrator",
        menuName = "FLFloppa/Save System/Migration/Sample Player Profile",
        order = 200)]
    public sealed class SamplePlayerProfileMigratorAsset : DataMigratorConfiguration
    {
        [SerializeField]
        [Tooltip("Source version this migrator upgrades from.")]
        private int _fromVersion = 1;

        [SerializeField]
        [Tooltip("Target version after the migration completes.")]
        private int _toVersion = 2;

        [SerializeField]
        [Tooltip("Optional default inventory item inserted during migration.")]
        private string _defaultItemId = "starter_sword";

        public override IDataMigrator Build()
        {
            return new SamplePlayerProfileMigrator(_fromVersion, _toVersion, _defaultItemId);
        }

        private sealed class SamplePlayerProfileMigrator : JsonTokenDataMigratorBase
        {
            private readonly string _defaultItemId;

            public SamplePlayerProfileMigrator(int fromVersion, int toVersion, string defaultItemId)
                : base(fromVersion, toVersion)
            {
                _defaultItemId = defaultItemId;
            }

            protected override JToken MigrateToken(JToken source)
            {
                if (source is not JObject obj)
                {
                    throw new SaveSystemException(
                        $"SamplePlayerProfileMigrator expected a JSON object but received {source.Type}.");
                }

                var inventory = obj["inventory"] as JArray;
                if (inventory == null)
                {
                    inventory = new JArray();
                    obj["inventory"] = inventory;
                }

                if (!string.IsNullOrWhiteSpace(_defaultItemId))
                {
                    inventory.Add(_defaultItemId);
                }

                obj["versionUpgraded"] = true;
                return obj;
            }
        }
    }
}

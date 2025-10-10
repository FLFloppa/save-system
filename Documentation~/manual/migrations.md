# Migration cookbook

When the structure of your save data changes you can migrate legacy payloads to the latest schema without breaking player progress. This document outlines common patterns when working with `IDataMigrator`.

## Concepts

* **FromVersion** – The version the migrator expects to receive.
* **ToVersion** – The version emitted after migration. This value must be greater than `FromVersion`.
* **SaveServiceConfiguration.CurrentVersion** – The latest version of your save schema. Every migration must advance the payload toward this value.

## Creating a migrator

The easiest way to implement a migrator is to inherit from `JsonTokenDataMigratorBase`.

```csharp
[CreateAssetMenu(menuName = "FLFloppa/Save System/Migrators/Player Inventory v1→v2")]
public sealed class PlayerInventoryV1ToV2 : JsonTokenDataMigratorBase
{
    public PlayerInventoryV1ToV2()
    {
        FromVersion = 1;
        ToVersion = 2;
    }

    protected override JToken Migrate(JToken token)
    {
        // add new field with a default value
        token["materials"] = new JArray();
        token["schemaVersion"] = ToVersion;
        return token;
    }
}
```

1. Create the asset via `Create → FLFloppa/Save System/Migrators/...`.
2. Add it to the `SaveServiceConfiguration.Migrators` list.
3. Ensure migrators are ordered by `FromVersion`. The inspector highlights gaps automatically.

## Chaining migrations

If your schema has evolved multiple times, create an asset per step.

```
v1 -> v2 -> v3 -> v4
```

At runtime the save system deserialises the payload, runs the processing pipeline, and applies each migrator from the current payload version to `CurrentVersion`.

## Handling breaking changes

* **Field removal** – Use `token.Remove("field")` to discard unused properties.
* **Type conversion** – Replace the token with a new structure (e.g., converting arrays to dictionaries).
* **Splitting payloads** – Consider extracting new metadata types or creating additional save keys rather than cramming everything into one payload.

## Testing migrations

Create edit-mode NUnit tests under `Packages/FLFloppa Save System/Tests/`:

```csharp
[Test]
public void InventoryMigration_UpgradesToVersion4()
{
    var legacy = JToken.Parse("{ \"schemaVersion\": 1, \"items\": [] }");
    var pipeline = new AllProcessingPipeline(Array.Empty<IProcessingModule>());
    var migrators = config.BuildMigratorDictionaryForEditor();
    var saveService = config.Build();

    var payload = config.Serializer.Build().SerializeFromToken(legacy);
    var migrated = migrators.Apply(payload, fromVersion: 1, targetVersion: 4);

    Assert.That(migrated.SchemaVersion, Is.EqualTo(4));
}
```

Automated tests catch regression before shipping and provide confidence that migrations behave as expected.

## Troubleshooting

* Unity inspector warns about gaps (`ToVersion` not matching the next `FromVersion`). Fix by adding the missing migrator or adjusting versions.
* If a migrator throws, the save system wraps the error in `SaveSystemException` with context about the failing module.

## Further reading

* [Manual index](index.md)
* [Readable payloads & metadata](readable-data.md)

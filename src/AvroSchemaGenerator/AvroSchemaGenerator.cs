using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AvroSchemaGenerator;

public sealed class AvroSchemaGenerator
{
    public static string Generate<T>(JsonSerializerOptions? options = null) =>
        Implementation.Generate(typeof(T)).ToJsonString(options ?? new() { WriteIndented = true });

    public static string Generate(Type type, JsonSerializerOptions? options = null) =>
        Implementation.Generate(type).ToJsonString(options);

    private readonly record struct Implementation(Dictionary<Type, JsonNode?> Schemas)
    {
        private static Dictionary<Type, JsonNode?> GetWellKnownSchemas() => new()
        {
            [typeof(bool)] = WellKnownSchemas.Boolean,
            [typeof(int)] = WellKnownSchemas.Int,
            [typeof(long)] = WellKnownSchemas.Long,
            [typeof(float)] = WellKnownSchemas.Float,
            [typeof(double)] = WellKnownSchemas.Double,
            [typeof(byte[])] = WellKnownSchemas.Bytes,
            [typeof(string)] = WellKnownSchemas.String,
            [typeof(DateOnly)] = WellKnownSchemas.Date,
            [typeof(TimeOnly)] = WellKnownSchemas.TimeMillis,
            [typeof(DateTime)] = WellKnownSchemas.TimestampMillis,
            [typeof(decimal)] = WellKnownSchemas.Decimal,

        };

        public static JsonNode Generate(Type type) =>
            new Implementation(GetWellKnownSchemas()).GenerateSchema(type);

        private JsonNode GenerateSchema(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (Schemas.TryGetValue(type, out var schemaName))
            {
                if (schemaName is null)
                {
                    throw new InvalidOperationException($"Circular reference detected for type {type.FullName}");
                }

                return schemaName.DeepClone();
            }

            if (type.IsNullableValueType())
            {
                return GenerateUnionSchema(isNullable: true, [Nullable.GetUnderlyingType(type)!]);
            }

            if (type.IsMap())
            {
                return GenerateMapSchema(type.GetGenericArguments()[1]);
            }

            if (type.IsArray())
            {
                return GenerateArraySchema(type.GetElementType() ?? type.GetGenericArguments()[0]);
            }

            if (type.IsEnum)
            {
                return GenerateEnumSchema(type);
            }

            // TODO: Can we do "fixed"?

            // TODO: We can do "error" by checking if it derives from Exception.

            return GenerateRecordSchema(type);
        }

        private JsonObject GenerateMapSchema(Type valuesType)
        {
            var map = JsonNode.Parse($$"""
            {
                "type": "map",
                "values": {{GenerateSchema(valuesType)}}
            }
            """)!.AsObject();

            return map;
        }

        private JsonObject GenerateArraySchema(Type itemsType)
        {
            var array = JsonNode.Parse($$"""
            {
                "type": "array",
                "items": {{GenerateSchema(itemsType)}}
            }
            """)!.AsObject();
            return array;
        }

        private JsonArray GenerateUnionSchema(bool isNullable, ReadOnlySpan<Type> types)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(types.Length);

            var union = JsonNode.Parse(isNullable ? "[\"null\"]" : "[]")!.AsArray();
            foreach (var type in types)
            {
                union.Add(GenerateSchema(type));
            }

            return union;
        }

        private JsonObject GenerateEnumSchema(Type type)
        {
            var @enum = JsonNode.Parse($$"""
            {
                "type": "enum",
                "name": "{{type.Name}}",
                "symbols": []
            }
            """)!.AsObject();

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                @enum["namespace"] = type.Namespace;
            }

            var symbols = @enum["symbols"]!.AsArray();
            foreach (var symbol in Enum.GetNames(type))
            {
                symbols.Add(symbol);
            }

            Schemas[type] = @enum["name"];

            return @enum;
        }

        private JsonObject GenerateRecordSchema(Type type)
        {
            var record = JsonNode.Parse($$"""
            {
                "type": "record",
                "name": "{{type.Name}}",
                "fields": []
            }
            """)!.AsObject();

            // We need to set the schema to null to avoid circular references.
            Schemas[type] = null;

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                record["namespace"] = type.Namespace;
            }

            var nullabilityInfoContext = new NullabilityInfoContext();
            var fields = record["fields"]!.AsArray();
            foreach (var property in type.GetProperties())
            {
                var field = JsonNode.Parse($$"""
            {
                "name": "{{property.Name}}"
            }
            """)!.AsObject();

                var nullabilityInfo = nullabilityInfoContext.Create(property);
                var isNullable = nullabilityInfo.WriteState == NullabilityState.Nullable;
                var nonNullableType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                field["type"] = isNullable
                    ? GenerateUnionSchema(isNullable, [nonNullableType])
                    : GenerateSchema(nonNullableType);

                fields.Add(field);
            }

            Schemas[type] = record["name"];

            return record;
        }
    }
}

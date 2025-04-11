using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AvroSchemaGenerator;

public sealed class AvroSchemaGenerator
{
    internal static readonly FrozenDictionary<Type, JsonNode> WellKnownMappings = new Dictionary<Type, JsonNode>()
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

    }.ToFrozenDictionary();

    public static string GenerateSchema<T>(JsonSerializerOptions? options = null) =>
        GenerateSchema(typeof(T)).ToJsonString(options ?? new() { WriteIndented = true });

    public static string GenerateSchema(Type type, JsonSerializerOptions? options = null) =>
        GenerateSchema(type).ToJsonString(options);

    internal static JsonNode GenerateSchema(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (WellKnownMappings.TryGetValue(type, out var schema))
        {
            return schema.DeepClone();
        }

        if (type.IsEnum)
        {
            return GenerateEnum(type);
        }

        if (type.IsNullableValueType())
        {
            return GenerateUnionSchema(isNullable: true, Nullable.GetUnderlyingType(type)!);
        }

        if (type.IsMap())
        {
            return GenerateMapSchema(type.GetGenericArguments()[1]);
        }

        if (type.IsArray())
        {
            return GenerateArraySchema(type.GetElementType() ?? type.GetGenericArguments()[0]);
        }

        // TODO: Can we do "fixed"?

        // TODO: We can do "error" by checking if it derives from Exception.

        return GenerateRecordSchema(type);
    }

    private static JsonObject GenerateEnum(Type type)
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

        return @enum;
    }

    private static JsonObject GenerateMapSchema(Type valuesType)
    {
        var map = JsonNode.Parse($$"""
        {
            "type": "map",
            "values": {{GenerateSchema(valuesType)}}
        }
        """)!.AsObject();

        return map;
    }

    private static JsonObject GenerateArraySchema(Type itemsType)
    {
        var array = JsonNode.Parse($$"""
        {
            "type": "array",
            "items": {{GenerateSchema(itemsType)}}
        }
        """)!.AsObject();
        return array;
    }

    private static JsonObject GenerateRecordSchema(Type type)
    {
        var record = JsonNode.Parse($$"""
        {
            "type": "record",
            "name": "{{type.Name}}",
            "fields": []
        }
        """)!.AsObject();

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
            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            field["type"] = isNullable
                ? GenerateUnionSchema(isNullable, underlyingType)
                : GenerateSchema(property.PropertyType);

            fields.Add(field);
        }

        return record;
    }

    private static JsonArray GenerateUnionSchema(bool isNullable, params ReadOnlySpan<Type> types)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(types.Length);

        var union = JsonNode.Parse(isNullable ? "[\"null\"]" : "[]")!.AsArray();
        foreach (var type in types)
        {
            union.Add(GenerateSchema(type));
        }

        return union;
    }
}

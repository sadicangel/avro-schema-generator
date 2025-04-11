using System.Text.Json.Nodes;

namespace AvroSchemaGenerator;

internal static class WellKnownSchemas
{
    public static JsonNode Boolean = Parse("\"boolean\"");
    public static JsonNode Int = Parse("\"int\"");
    public static JsonNode Long = Parse("\"long\"");
    public static JsonNode Float = Parse("\"float\"");
    public static JsonNode Double = Parse("\"double\"");
    public static JsonNode Bytes = Parse("\"bytes\"");
    public static JsonNode String = Parse("\"string\"");

    public static readonly JsonNode Date = Parse("""
    {
      "type": "int",
      "logicalType": "date"
    }
    """);
    public static readonly JsonNode Decimal = Parse("""
    {
      "type": "bytes",
      "logicalType": "decimal",
      "precision": 4,
      "scale": 2
    }
    """);
    public static readonly JsonNode TimeMillis = Parse("""
    {
      "type": "long",
      "logicalType": "time-millis"
    }
    """);
    public static readonly JsonNode TimeMicros = Parse("""
    {
      "type": "long",
      "logicalType": "time-micros"
    }
    """);
    public static readonly JsonNode TimestampMillis = Parse("""
    {
      "type": "long",
      "logicalType": "timestamp-millis"
    }
    """);
    public static readonly JsonNode TimestampMicros = Parse("""
    {
      "type": "long",
      "logicalType": "timestamp-micros"
    }
    """);
    public static readonly JsonNode LocalTimestampMillis = Parse("""
    {
      "type": "long",
      "logicalType": "local-timestamp-millis"
    }
    """);
    public static readonly JsonNode LocalTimestampMicros = Parse("""
    {
      "type": "long",
      "logicalType": "local-timestamp-micros"
    }
    """);
    public static readonly JsonNode Uuid = Parse("""
    {
      "type": "fixed",
      "size": 16,
      "name": "uuid"
    }
    """);

    private static JsonNode Parse(string schema) =>
        JsonNode.Parse(schema) ?? throw new InvalidOperationException("Failed to parse schema.");
}

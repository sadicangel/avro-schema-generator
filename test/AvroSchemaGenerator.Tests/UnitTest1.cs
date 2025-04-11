namespace AvroSchemaGenerator.Tests;

public class UnitTest1
{
    public record Test(
        string Name,
        int Age,
        DateTime Date,
        TimeOnly Time,
        DateOnly DateOnly,
        decimal Decimal,
        bool? NullableBool,
        string? NullableString);

    [Fact]
    public void Test1()
    {
        var schema = AvroSchemaGenerator.GenerateSchema<Test>();
        Assert.NotNull(schema);
    }
}

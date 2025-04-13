namespace AvroSchemaGenerator.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var schema = AvroSchemaGenerator.Generate<Test>();
        Assert.NotNull(schema);
    }
}

public record Test(
    string Name,
    int Age,
    DateTime Date,
    TimeOnly Time,
    DateOnly DateOnly,
    decimal Decimal,
    bool? NullableBool,
    string? NullableString,
    TestEnum Enum1,
    TestEnum Enum2);

public enum TestEnum
{
    Value1,
    Value2,
    Value3,
}

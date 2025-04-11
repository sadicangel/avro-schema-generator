using System.Collections;

namespace AvroSchemaGenerator;

internal static class TypeExtensions
{
    public static bool IsNullableValueType(this Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    // TODO: Key should only be convertible to string.
    public static bool IsMap(this Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableTo(typeof(IDictionary));

    public static bool IsArray(this Type type) =>
        type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IEnumerable));
}

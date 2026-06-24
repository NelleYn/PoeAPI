using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ExileCore.Shared.Nodes;
public static class JsonSerializationHelper
{
    private static readonly Type SerializerProxyType;
    private static readonly Type InternalSerializerType;
    private static readonly MethodInfo IntSerMethod;
    private static readonly MethodInfo CreateMethod;
    private static readonly FieldInfo UnwrappedSerializerField;
    public static JsonSerializer Unwrap(JsonSerializer serializer)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public static T DeserializeDefaultValue<T>(JsonReader reader, Type objectType, T existingValue, JsonSerializer serializer)
    {
        object obj = IntSerMethod.Invoke(serializer, Array.Empty<object>());
        JsonContract jsonContract = serializer.ContractResolver.ResolveContract(objectType);
        return (T)CreateMethod.Invoke(obj, new object[7] { reader, objectType, jsonContract, null, null, null, existingValue });
    }

    static JsonSerializationHelper()
    {
        _ = typeof(JsonReader).TypeHandle;
        _ = typeof(JsonReader).TypeHandle;
        _ = 36;
        _ = 36;
        _ = 36;
    }
}
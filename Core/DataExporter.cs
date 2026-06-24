using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExileCore;
public class DataExporter
{
    public static string ExportDataBase64(object data, string tag, JsonSerializerSettings serializerSettings)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public static JToken ImportDataBase64(string serializedData, string tag)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private static JObject GetBase64JObject(string serializedData)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public static T ImportDataBase64<T>(string serializedData, string tag, JsonSerializerSettings serializerSettings)
    {
        return ImportDataBase64(serializedData, tag).ToObject<T>(JsonSerializer.Create(serializerSettings));
    }

    public static string ExportDataJson(object data, string tag, JsonSerializerSettings serializerSettings)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public static JToken ImportDataJson(string serializedData, string tag)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public static T ImportDataJson<T>(string serializedData, string tag, JsonSerializerSettings serializerSettings)
    {
        return ImportDataJson(serializedData, tag).ToObject<T>(JsonSerializer.Create(serializerSettings));
    }

    public static string PeekDataTagBase64(string serializedData)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public static string PeekDataTagJson(string serializedData)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExileCore.Shared.Nodes
{
    /// <summary>
    /// Persists a <see cref="ContentNode{T}"/> as a plain JSON array of its items, and reads it
    /// back <b>into the existing node</b> rather than replacing it.
    /// </summary>
    /// <typeparam name="T">The item type of the node being converted.</typeparam>
    /// <remarks>
    /// Reusing the instance is the whole point: <see cref="ContentNode{T}.ItemFactory"/> and
    /// <see cref="ContentNode{T}.OnRemove"/> are delegates supplied by the property initializer and
    /// cannot be serialized, so a node built fresh by the deserializer would come back without its
    /// factory and the menu's "add" button would be gone after the first restart.
    /// </remarks>
    public class ContentNodeConverter<T> : JsonConverter<ContentNode<T>>
    {
        /// <summary>Always writes through this converter.</summary>
        public override bool CanWrite => true;

        /// <summary>Writes the node as a JSON array of its items.</summary>
        public override void WriteJson(JsonWriter writer, ContentNode<T> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value?.Content ?? new List<T>());
        }

        /// <summary>Reads the item array into <paramref name="existingValue"/> when there is one.</summary>
        public override ContentNode<T> ReadJson(JsonReader reader, Type objectType, ContentNode<T> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var node = hasExistingValue && existingValue != null ? existingValue : new ContentNode<T>();
            if (reader.TokenType == JsonToken.Null) return node;
            node.Content = serializer.Deserialize<List<T>>(reader);
            return node;
        }
    }

    /// <summary>
    /// The type-erased entry point for <see cref="ContentNodeConverter{T}"/>, applied to
    /// <see cref="ContentNode{T}"/> itself so that any serializer handles content nodes correctly,
    /// whether or not it was configured with <c>SettingsContainer.jsonSettings</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonConverterAttribute"/> instantiates the converter type directly, so it cannot
    /// name an open generic. This class closes <see cref="ContentNodeConverter{T}"/> over the item
    /// type at runtime and forwards to it, caching one converter per item type.
    /// </remarks>
    public class ContentNodeConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, JsonConverter> Converters = new ConcurrentDictionary<Type, JsonConverter>();

        /// <summary>Always writes through this converter.</summary>
        public override bool CanWrite => true;

        /// <summary>Always reads through this converter.</summary>
        public override bool CanRead => true;

        /// <summary>Whether the given type is some <see cref="ContentNode{T}"/>.</summary>
        public override bool CanConvert(Type objectType)
        {
            return TryGetItemType(objectType, out _);
        }

        /// <summary>Forwards to the typed converter for the value's item type.</summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            GetConverter(value.GetType()).WriteJson(writer, value, serializer);
        }

        /// <summary>Forwards to the typed converter for the target's item type.</summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return GetConverter(existingValue?.GetType() ?? objectType).ReadJson(reader, objectType, existingValue, serializer);
        }

        private static JsonConverter GetConverter(Type contentNodeType)
        {
            return Converters.GetOrAdd(contentNodeType, type =>
            {
                if (!TryGetItemType(type, out var itemType))
                    throw new JsonSerializationException($"{type} is not a {typeof(ContentNode<>)}.");

                return (JsonConverter) Activator.CreateInstance(typeof(ContentNodeConverter<>).MakeGenericType(itemType));
            });
        }

        private static bool TryGetItemType(Type type, out Type itemType)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ContentNode<>))
                {
                    itemType = current.GetGenericArguments()[0];
                    return true;
                }
            }

            itemType = null;
            return false;
        }
    }
}

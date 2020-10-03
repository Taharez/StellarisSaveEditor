using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StellarisSaveEditor.BlazorClient.Helpers
{
    public class DictionaryTKeyInt32TValueConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            if (typeToConvert.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            {
                return false;
            }

            return typeToConvert.GetGenericArguments()[0] == typeof(int);
        }

        public override JsonConverter CreateConverter(
            Type type,
            JsonSerializerOptions options)
        {
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(DictionaryInt32ConverterInner<>).MakeGenericType(
                    new Type[] { valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null);

            return converter;
        }

        private class DictionaryInt32ConverterInner<TValue> :
            JsonConverter<Dictionary<int, TValue>>
        {
            private readonly JsonConverter<TValue> _valueConverter;
            private Type _keyType;
            private Type _valueType;

            public DictionaryInt32ConverterInner(JsonSerializerOptions options)
            {
                // For performance, use the existing converter if available.
                _valueConverter = (JsonConverter<TValue>)options
                    .GetConverter(typeof(TValue));

                // Cache the key and value types.
                _keyType = typeof(int);
                _valueType = typeof(TValue);
            }

            public override Dictionary<int, TValue> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                var dictionary = new Dictionary<int, TValue>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return dictionary;
                    }

                    // Get the key.
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string propertyName = reader.GetString();

                    if (!int.TryParse(propertyName, out var key))
                    {
                        throw new JsonException(
                            $"Unable to convert \"{propertyName}\" to int \"{_keyType}\".");
                    }

                    // Get the value.
                    TValue v;
                    if (_valueConverter != null)
                    {
                        reader.Read();
                        v = _valueConverter.Read(ref reader, _valueType, options);
                    }
                    else
                    {
                        v = JsonSerializer.Deserialize<TValue>(ref reader, options);
                    }

                    // Add to dictionary.
                    dictionary.Add(key, v);
                }

                throw new JsonException();
            }

            public override void Write(
                Utf8JsonWriter writer,
                Dictionary<int, TValue> dictionary,
                JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (KeyValuePair<int, TValue> kvp in dictionary)
                {
                    writer.WritePropertyName(kvp.Key.ToString());

                    if (_valueConverter != null)
                    {
                        _valueConverter.Write(writer, kvp.Value, options);
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, kvp.Value, options);
                    }
                }

                writer.WriteEndObject();
            }
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenFFBoardPlugin.Utils
{
    internal class JsonHandler
    {
        private static readonly JsonSerializerSettings SerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        public static T LoadFromJsonFile<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), SerSettings);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"Failed to load json from {path}: {ex.Message}");
            }

            return null;
        }

        public static void SaveToJsonFile<T>(string path, T obj) where T : class
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, SerSettings));
        }

        public static T Clone<T>(T obj) where T : class
        {
            var json = JsonConvert.SerializeObject(obj, SerSettings);
            return JsonConvert.DeserializeObject<T>(json, SerSettings);
        }
    }

    /// <summary>
    /// Tolerant converter for List&lt;T&gt; that accepts a JSON array, an empty object {},
    /// or null — all treated as an empty list when the token is not an array.
    /// </summary>
    internal class FlexibleListConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> ReadJson(JsonReader reader, Type objectType, List<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Array)
                return token.ToObject<List<T>>(serializer);

            // {} or null or anything else → empty list
            return new List<T>();
        }

        public override void WriteJson(JsonWriter writer, List<T> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}

using System;
using Newtonsoft.Json;

namespace AElf.Configuration
{
    public class JsonSerializer : ISerializer
    {
        public static readonly ISerializer Instance = new JsonSerializer();

        private JsonSerializer()
        {
        }

        public string Serialize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return json;
        }

        public T Deserialize<T>(string value) where T : class
        {
            var obj = JsonConvert.DeserializeObject<T>(value);
            return obj;
        }

        public object Deserialize(string value, Type type)
        {
            var obj = JsonConvert.DeserializeObject(value, type);
            return obj;
        }
    }
}
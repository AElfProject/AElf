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

        public T Deserialize<T>(string vaule) where T : class
        {
            var obj = JsonConvert.DeserializeObject<T>(vaule);
            return obj;
        }

        public object Deserialize(string vaule, Type type)
        {
            var obj = JsonConvert.DeserializeObject(vaule, type);
            return obj;
        }
    }
}
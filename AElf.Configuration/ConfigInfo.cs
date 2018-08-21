using System;

namespace AElf.Configuration
{
    public class ConfigInfo
    {
        private string _name;
        public Type Type { get; }
        public object Value { get; }

        public ConfigInfo(string name, Type type, string content)
        {
            _name = name;
            Type = type;
            if (string.IsNullOrWhiteSpace(content))
            {
                Value = CreateDefaultInstance(type);
            }
            else
            {
                try
                {
                    Value = JsonSerializer.Instance.Deserialize(content, type);
                }
                catch (Exception e)
                {
                    //Todo log error
                    Value = CreateDefaultInstance(type);
                }
            }
        }

        private object CreateDefaultInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}
using System;

namespace AElf.Configuration
{
    public class ConfigInfo
    {
        private string _name;
        private Type _type;
        private object _value;

        public object Value
        {
            get { return _value; }
        }

        public ConfigInfo(string name, Type type, string content)
        {
            _name = name;
            _type = type;
            if (string.IsNullOrWhiteSpace(content))
            {
                _value = CreateDefaultInstance(type);
            }
            else
            {
                try
                {
                    _value = JsonSerializer.Instance.Deserialize(content, type);
                }
                catch (Exception e)
                {
                    //Todo log error
                    _value = CreateDefaultInstance(type);
                }
            }
        }

        private object CreateDefaultInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}
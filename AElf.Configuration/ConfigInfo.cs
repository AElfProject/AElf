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
            _value = JsonSerializer.Instance.Deserialize(content, type);
        }
    }
}
using System;

namespace AElf.Api.CSharp
{
    [AttributeUsage(AttributeTargets.All)]
    public class DataProviderNameAttribute : Attribute
    {
        private string _name;

        public DataProviderNameAttribute(string name)
        {
            this._name = name;
        }

        public virtual string Name
        {
            get {return _name;}
        }
    }
}
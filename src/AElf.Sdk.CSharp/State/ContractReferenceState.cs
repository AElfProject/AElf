using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Sdk.CSharp.State
{
    public class ContractReferenceState : SingletonState<Address>
    {
        private Dictionary<string, PropertyInfo> _methodReferenceProperties;

        public ContractReferenceState()
        {
            DetectPropertyInfos();
            InitializeProperties();
        }

        private void DetectPropertyInfos()
        {
            _methodReferenceProperties = this.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.PropertyType.IsMethodReference())
                .ToDictionary(x => x.Name, x => x);
        }

        private void InitializeProperties()
        {
            foreach (var kv in _methodReferenceProperties)
            {
                var name = kv.Key;
                var propertyInfo = kv.Value;
                var propertyType = kv.Value.PropertyType;
                var instance = Activator.CreateInstance(propertyType, this, name);
                propertyInfo.SetValue(this, instance);
            }
        }
    }
}
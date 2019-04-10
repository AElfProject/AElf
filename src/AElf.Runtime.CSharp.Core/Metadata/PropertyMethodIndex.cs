using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Metadata
{
    public class PropertyMethodIndex
    {
        private readonly IDictionary<MethodDefinition, PropertyDefinition> _cache =
            new Dictionary<MethodDefinition, PropertyDefinition>();

        public PropertyMethodIndex(IEnumerable<PropertyDefinition> propertyDefinitions)
        {
            AddPropertyDefinitions(propertyDefinitions);
        }

        public void AddPropertyDefinitions(IEnumerable<PropertyDefinition> propertyDefinitions)
        {
            foreach (var property in propertyDefinitions)
            {
                AddPropertyDefinition(property);
            }
        }

        public void AddPropertyDefinition(PropertyDefinition propertyDefinition)
        {
            if (propertyDefinition.SetMethod != null)
            {
                _cache[propertyDefinition.SetMethod] = propertyDefinition;
            }

            if (propertyDefinition.GetMethod != null)
            {
                _cache[propertyDefinition.GetMethod] = propertyDefinition;
            }
        }

        public PropertyDefinition this[MethodDefinition methodDefinition]
        {
            get
            {
                _cache.TryGetValue(methodDefinition, out var propertyDefinition);
                return propertyDefinition;
            }
        } 
    }
}
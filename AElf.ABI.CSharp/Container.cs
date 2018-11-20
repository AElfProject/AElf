using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AElf.ABI.CSharp
{
    public class Container
    {
        public TypeReference SdkBaseContract = null;
        private string _contractBaseFullName;
        private string _eventBaseFullName;
        private string _typeBaseFullName;

        public List<TypeDefinition> Types { get; } = new List<TypeDefinition>();
        public List<TypeDefinition> Events { get; } = new List<TypeDefinition>();

        private readonly Dictionary<string, List<TypeDefinition>> _baseChildrenMap =
            new Dictionary<string, List<TypeDefinition>>();
        
        private readonly Dictionary<string, TypeDefinition> _nameTypeDefinitions =
            new Dictionary<string, TypeDefinition>();

        public Container(string contractBaseFullName, string eventBaseFullName, string typeBaseFullName)
        {
            _contractBaseFullName = contractBaseFullName;
            _eventBaseFullName = eventBaseFullName;
            _typeBaseFullName = typeBaseFullName;
        }

        private void AddOneType(TypeDefinition type)
        {
            string baseName = type.BaseType?.FullName;
            if (baseName == null)
                return;
            if (!_baseChildrenMap.TryGetValue(baseName, out var children))
            {
                children = new List<TypeDefinition>();
                _baseChildrenMap.Add(baseName, children);
            }

            children.Add(type);
            if (baseName == _contractBaseFullName)
            {
                SdkBaseContract = type.BaseType;
            }
            else if (baseName == _eventBaseFullName)
            {
                Events.Add(type);
            }
            else if (baseName == _typeBaseFullName)
            {
                Types.Add(type);
            }
            if(!_nameTypeDefinitions.ContainsKey(type.FullName))
                _nameTypeDefinitions.Add(type.FullName, type);
        }

        public void AddType(TypeDefinition type, bool includingNested = true)
        {
            AddOneType(type);
            if (!includingNested)
                return;
            if (!type.HasNestedTypes)
                return;
            foreach (var nt in type.NestedTypes)
            {
                AddType(nt);
            }
        }

        public IEnumerable<TypeDefinition> GetSmartContractTypePath(string name = null)
        {
            return name != null ? GetTypePathWithName(name) : GetTypePathWithoutName();
        }


        /// <summary>
        /// return type definiations with specific name and classes inherit it
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IEnumerable<TypeDefinition> GetTypePathWithName(string name)
        {
            var children = new List<TypeDefinition>();
            try
            {
                var fullName = _nameTypeDefinitions.First(kv => kv.Key.Contains(name)).Key;
                children.Add(_nameTypeDefinitions[fullName]);
            }
            catch (Exception e)
            {
                throw new Exception("No valid smart contract found.", e);
            }
            
            return GetTypePaths(children);

        }

        private IEnumerable<TypeDefinition> GetTypePathWithoutName()
        {
            if (_contractBaseFullName == null)
                return null;
            var curName = _contractBaseFullName;
            if (!_baseChildrenMap.TryGetValue(curName, out var children))
            {
                throw new Exception("No valid smart contract found.");
            }
            return GetTypePaths(children);
        }

        
        private IEnumerable<TypeDefinition> GetTypePaths(List<TypeDefinition> children)
        {
            var types = new List<TypeDefinition>();
            Queue<TypeDefinition> queue = new Queue<TypeDefinition>(children);

            while (queue.Count != 0)
            {
                var type = queue.Dequeue();
                types.Add(type);
                if (!_baseChildrenMap.TryGetValue(type.FullName, out children))
                {
                    continue;
                }

                foreach (var typeDefinition in children)
                    queue.Enqueue(typeDefinition);
            }

            return types;
        }
}
}

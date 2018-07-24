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

        public List<TypeReference> TypeReferences { get; private set; }
        public List<TypeDefinition> Types { get; } = new List<TypeDefinition>();
        public List<TypeDefinition> Events { get; } = new List<TypeDefinition>();
        public Dictionary<string, List<TypeDefinition>> _baseChildrenMap = new Dictionary<string, List<TypeDefinition>>();

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
            }else if (baseName == _typeBaseFullName)
            {
                Types.Add(type);
            }
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

        public IEnumerable<TypeDefinition> GetSmartContractTypePath()
        {
            var types = new List<TypeDefinition>();
            if (_contractBaseFullName == null)
                return null;
            var curName = _contractBaseFullName;
            if (!_baseChildrenMap.TryGetValue(curName, out var children))
            {
                throw new Exception("No valid smart contract found.");
            }
            while (true)
            {
                var contractType = children.Count > 1
                    ? children.First(c => c.FullName.Contains(Kernel.Globals.ConsensusType.ToString())) //To select the specific consensus contract
                    : children[0];
                types.Add(contractType);
                curName = contractType.FullName;
                if (!_baseChildrenMap.TryGetValue(curName, out children))
                {
                    return types;
                }
            }
        }
    }
}

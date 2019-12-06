using AElf.Sdk.CSharp;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps
{
    public static class Extensions
    {
        public static bool IsContractImplementation(this TypeDefinition typeDefinition)
        {
            while (true)
            {
                var baseType = typeDefinition.BaseType.Resolve();
                
                if (baseType.BaseType == null) // Reached the type before object type (the most base type)
                {
                    return typeDefinition.FullName == typeof(CSharpSmartContract).FullName;
                }

                typeDefinition = baseType;
            }
        }
    }
}

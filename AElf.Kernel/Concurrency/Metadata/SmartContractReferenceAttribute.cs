using System;

namespace AElf.Kernel.Concurrency.Metadata
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SmartContractReferenceAttribute : Attribute
    {
        public SmartContractReferenceAttribute(string fieldName, Type contractType)
        {
            FieldName = fieldName;
            ContractType = contractType;
        }

        public string FieldName { get; }
        public Type ContractType { get; }
    }
}
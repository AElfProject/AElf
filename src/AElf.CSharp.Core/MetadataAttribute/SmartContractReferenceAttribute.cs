using System;
using AElf.Kernel;

namespace AElf.CSharp.Core.MetadataAttribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SmartContractReferenceAttribute : Attribute
    {
        public SmartContractReferenceAttribute(string fieldName, string contractAddr)
        {
            FieldName = fieldName;
            ContractAddress = contractAddr;
        }

        public string FieldName { get; }
        public string ContractAddress { get; } //TODO: check whether there is right contract in the target address
    }
}
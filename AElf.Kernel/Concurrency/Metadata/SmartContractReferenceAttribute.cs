using System;

namespace AElf.Kernel.Concurrency.Metadata
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SmartContractReferenceAttribute : Attribute
    {
        public SmartContractReferenceAttribute(string fieldName, string className)
        {
            FieldName = fieldName;
            ClassName = className;
        }

        public string FieldName { get; }
        public string ClassName { get; }
    }
}
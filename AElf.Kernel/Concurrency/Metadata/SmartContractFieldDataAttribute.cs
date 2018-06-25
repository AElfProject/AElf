using System;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency.Metadata
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SmartContractFieldDataAttribute : Attribute
    {
        public SmartContractFieldDataAttribute(string fieldName, DataAccessMode dataAccessMode)
        {
            DataAccessMode = dataAccessMode;
            FieldName = fieldName;
        }

        public DataAccessMode DataAccessMode { get; }
        public string FieldName { get; }
    }
}
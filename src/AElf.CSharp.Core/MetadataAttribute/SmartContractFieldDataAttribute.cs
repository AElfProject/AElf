using System;
using AElf.Kernel;

namespace AElf.CSharp.Core.MetadataAttribute
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
using System;

namespace AElf.Kernel.Concurrency
{
    public enum DataAccessMode{
        ReadOnlyAccountSharing,
        ReadWriteAccountSharing,
        AccountSpecific
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SmartContractFieldDataAttribute : Attribute
    {
        public SmartContractFieldDataAttribute(string dataName, DataAccessMode dataAccessMode)
        {
            DataAccessMode = dataAccessMode;
            DataFullName = dataName;
        }

        public DataAccessMode DataAccessMode { get; }
        public string DataFullName { get; }
    }
}
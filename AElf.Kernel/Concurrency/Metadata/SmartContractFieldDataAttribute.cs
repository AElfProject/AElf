using System;

namespace AElf.Kernel.Concurrency
{
    public enum DataAccessMode{
        ReadOnlyAccountSharing,
        ReadWriteAccountSharing,
        AccountSpecific
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class SmartContractFieldDataAttribute : Attribute
    {
        public SmartContractFieldDataAttribute(string dataFullName, DataAccessMode dataAccessMode)
        {
            DataAccessMode = dataAccessMode;
            DataFullName = dataFullName;
        }

        public DataAccessMode DataAccessMode { get; }
        public string DataFullName { get; }
    }
}
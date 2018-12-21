using System;

namespace AElf.Kernel.SmartContract
{
    public partial class Resource
    {
        public Resource(string name, DataAccessMode dataAccessMode)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataAccessMode = dataAccessMode;
        }
    }
}
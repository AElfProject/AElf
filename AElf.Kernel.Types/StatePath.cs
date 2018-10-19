using System;
using AElf.Common;

namespace AElf.Kernel
{
    public partial class StatePath
    {
        public Hash GetHash()
        {
            if (ChainId == null)
            {
                throw new Exception($"{nameof(ChainId)} is null.");
            }

            if (ContractAddress == null)
            {
                throw new Exception($"{nameof(ContractAddress)} is null.");
            }

            if (Path == null)
            {
                throw new Exception($"{nameof(Path)} is null.");
            }

            return Hash.FromMessage(this);
        }
    }
}
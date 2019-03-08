using System;
using AElf.Common;

namespace AElf.Kernel
{
    public partial class StatePath : IHashProvider
    {
        public Hash GetHash()
        {
            if (Path == null)
            {
                throw new Exception($"{nameof(Path)} is null.");
            }

            return Hash.FromMessage(this);
        }
    }
}
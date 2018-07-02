﻿using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public class AccountDataContext : IAccountDataContext
    {
        public ulong IncrementId { get; set; }
        public Hash Address { get; set; }
        public Hash ChainId { get; set; }

        public Hash GetHash()
        {
            return ChainId.CalculateHashWith(Address);
        }
    }
}
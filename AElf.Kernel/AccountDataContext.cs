﻿namespace AElf.Kernel
{
    public class AccountDataContext : IAccountDataContext
    {
        public ulong IncreasementId { get; set; }
        public Hash Address { get; set; }
        public Hash ChainId { get; set; }
    }
}
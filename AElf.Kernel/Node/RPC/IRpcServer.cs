﻿namespace AElf.Kernel.Node.RPC
{
    public interface IRpcServer
    {
        bool Start();
        void SetCommandContext(MainChainNode node);
    }
}
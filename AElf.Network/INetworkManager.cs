using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;

namespace AElf.Network
{
    public interface INetworkManager
    {
        event EventHandler MessageReceived;
        
        Task Start();

        Task<int> BroadcastBlock(byte[] hash, byte[] payload);
    }
}
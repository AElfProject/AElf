﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using Anemonis.AspNetCore.JsonRpc;

#pragma warning disable 1998

namespace AElf.OS.Rpc.Net
{
    [Path("/net")]
    public class NetRpcService : IJsonRpcService
    {
        public INetworkService NetworkService { get; set; }

        [JsonRpcMethod("AddPeer", "address")]
        public async Task<bool> AddPeer(string address)
        {
            return await NetworkService.AddPeerAsync(address);
        }

        [JsonRpcMethod("RemovePeer", "address")]
        public async Task<bool> RemovePeer(string address)
        {
            return await NetworkService.RemovePeerAsync(address);
        }

        [JsonRpcMethod("GetPeers")]
        public async Task<List<string>> GetPeers()
        {
            return NetworkService.GetPeers();
        }
    }
}
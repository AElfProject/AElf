using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Consensus.Application;

public interface IBlockConfirmationService
{
    Task CollectBlockConfirmationAsync(string peerPubkey, Hash blockHash, long blockHeight, byte[] signature);
    Task<(BlockIndex, Dictionary<string, byte[]>)> GetLatestConfirmedBlockAsync();
    Task<bool> VerifyBlsSignatureAsync(byte[] signature, byte[] data, string peerPubkey);
}
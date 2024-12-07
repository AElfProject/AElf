using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography.Bls;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

internal class BlockConfirmationService : IBlockConfirmationService
{
    private readonly IConsensusReaderContextService _consensusReaderContextService;

    private readonly IContractReaderFactory<AEDPoSContractImplContainer.AEDPoSContractImplStub>
        _contractReaderFactory;

    private readonly IBlockchainService _blockchainService;

    private readonly ConcurrentDictionary<BlockIndex, ConcurrentDictionary<string, byte[]>>
        _blockConfirmationSignatures = new();

    public BlockConfirmationService(
        IBlockchainService blockchainService,
        IContractReaderFactory<AEDPoSContractImplContainer.AEDPoSContractImplStub> contractReaderFactory,
        IConsensusReaderContextService consensusReaderContextService)
    {
        _blockchainService = blockchainService;
        _contractReaderFactory = contractReaderFactory;
        _consensusReaderContextService = consensusReaderContextService;
    }

    public async Task CollectBlockConfirmationAsync(string peerPubkey, Hash blockHash, long blockHeight,
        byte[] signature)
    {
        var blockIndex = new BlockIndex
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight
        };

        var signatures = _blockConfirmationSignatures.GetOrAdd(blockIndex, new ConcurrentDictionary<string, byte[]>());
        signatures[peerPubkey] = signature;

        await CleanUpOldDataAsync();
    }

    public async Task<(BlockIndex, Dictionary<string, byte[]>)> GetLatestConfirmedBlockAsync()
    {
        var requiredSignaturesCount = await GetRequiredSignaturesCountAsync();
        var confirmedBlock = _blockConfirmationSignatures
            .Where(kv => kv.Value.Count >= requiredSignaturesCount)
            .OrderByDescending(kv => kv.Value.Count)
            .ThenByDescending(kv => kv.Key)
            .FirstOrDefault();

        return (confirmedBlock.Key, confirmedBlock.Value.ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    private async Task<int> GetRequiredSignaturesCountAsync()
    {
        var chain = await _blockchainService.GetChainAsync();
        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            });
        var minerList =
            await _contractReaderFactory
                .Create(contractReaderContext).GetCurrentMinerList
                .CallAsync(new Empty());
        var minersCount = minerList.Pubkeys.Count;
        return minersCount * 2 / 3 + 1;
    }

    public async Task<bool> VerifyBlsSignatureAsync(byte[] signature, byte[] data, string peerPubkey)
    {
        var blsPubkey = await GetBlsPubkeyAsync(peerPubkey);
        if (blsPubkey != null)
        {
            return BlsHelper.VerifySignature(signature, data, blsPubkey);
        }

        return false;
    }

    private async Task<byte[]> GetBlsPubkeyAsync(string peerPubkey)
    {
        var chain = await _blockchainService.GetChainAsync();
        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            });
        var blsPubkey =
            await _contractReaderFactory
                .Create(contractReaderContext).GetBlsPubkey
                .CallAsync(Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(peerPubkey)));
        return blsPubkey.ToByteArray();
    }

    private async Task CleanUpOldDataAsync()
    {
        var maxBlockHeight = _blockConfirmationSignatures.Keys.Max(k => k.BlockHeight);
        var keysToRemove = _blockConfirmationSignatures.Keys.Where(k => k.BlockHeight < maxBlockHeight).ToList();

        foreach (var key in keysToRemove)
        {
            _blockConfirmationSignatures.TryRemove(key, out _);
        }
    }
}
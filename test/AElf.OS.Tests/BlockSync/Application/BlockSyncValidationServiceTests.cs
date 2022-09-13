using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application;

public class BlockSyncValidationServiceTests : BlockSyncTestBase
{
    private readonly IAccountService _accountService;
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockSyncValidationService _blockSyncValidationService;
    private readonly OSTestHelper _osTestHelper;

    public BlockSyncValidationServiceTests()
    {
        _blockSyncValidationService = GetRequiredService<IBlockSyncValidationService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _accountService = GetRequiredService<IAccountService>();
        _osTestHelper = GetRequiredService<OSTestHelper>();
    }

    [Fact]
    public async Task ValidateAnnouncement_Success()
    {
        var chain = await _blockchainService.GetChainAsync();

        var blockAnnouncement = new BlockAnnouncement
        {
            BlockHash = HashHelper.ComputeFrom("SyncBlockHash"),
            BlockHeight = chain.LastIrreversibleBlockHeight + 1
        };

        var validateResult =
            await _blockSyncValidationService.ValidateAnnouncementBeforeSyncAsync(chain, blockAnnouncement,
                GetEncodedPubKeyString());

        validateResult.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAnnouncement_AlreadySynchronized()
    {
        var chain = await _blockchainService.GetChainAsync();

        var blockAnnouncement = new BlockAnnouncement
        {
            BlockHash = HashHelper.ComputeFrom("SyncBlockHash"),
            BlockHeight = chain.LastIrreversibleBlockHeight + 1
        };

        var validateResult =
            await _blockSyncValidationService.ValidateAnnouncementBeforeSyncAsync(chain, blockAnnouncement,
                GetEncodedPubKeyString());
        validateResult.ShouldBeTrue();

        validateResult =
            await _blockSyncValidationService.ValidateAnnouncementBeforeSyncAsync(chain, blockAnnouncement,
                GetEncodedPubKeyString());
        validateResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAnnouncement_LessThenLIBHeight()
    {
        var chain = await _blockchainService.GetChainAsync();

        var blockAnnouncement = new BlockAnnouncement
        {
            BlockHash = HashHelper.ComputeFrom("SyncBlockHash"),
            BlockHeight = chain.LastIrreversibleBlockHeight
        };

        var validateResult =
            await _blockSyncValidationService.ValidateAnnouncementBeforeSyncAsync(chain, blockAnnouncement,
                GetEncodedPubKeyString());

        validateResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateBlock_Success()
    {
        var chain = await _blockchainService.GetChainAsync();

        var block = _osTestHelper.GenerateBlockWithTransactions(chain.LastIrreversibleBlockHash,
            chain.LastIrreversibleBlockHeight);
        var pubkey = (await _accountService.GetPublicKeyAsync()).ToHex();

        var validateResult = await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, block, pubkey);

        validateResult.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBlock_LessThenLIBHeight()
    {
        var chain = await _blockchainService.GetChainAsync();

        var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("SyncBlockHash"),
            chain.LastIrreversibleBlockHeight - 1);
        var pubkey = (await _accountService.GetPublicKeyAsync()).ToHex();

        var validateResult = await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, block, pubkey);

        validateResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateBlock_IncorrectSender()
    {
        var chain = await _blockchainService.GetChainAsync();

        var block = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("SyncBlockHash"),
            chain.LastIrreversibleBlockHeight + 1);

        var validateResult =
            await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, block, GetEncodedPubKeyString());

        validateResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateBlockBeforeAttach_Success()
    {
        var chain = await _blockchainService.GetChainAsync();
        var transactions = await _osTestHelper.GenerateTransferTransactions(3);
        var block = _osTestHelper.GenerateBlockWithTransactions(chain.BestChainHash, chain.BestChainHeight,
            transactions);

        var result = await _blockSyncValidationService.ValidateBlockBeforeAttachAsync(block);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBlockBeforeAttach_InvalidTransaction_ReturnFalse()
    {
        var chain = await _blockchainService.GetChainAsync();
        var transaction = await _osTestHelper.GenerateTransferTransaction();
        transaction.Signature = ByteString.Empty;
        var block = _osTestHelper.GenerateBlockWithTransactions(chain.BestChainHash, chain.BestChainHeight,
            new[] { transaction });

        var result = await _blockSyncValidationService.ValidateBlockBeforeAttachAsync(block);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateBlockBeforeAttach_ExpiredTransaction_ReturnFalse()
    {
        var chain = await _blockchainService.GetChainAsync();
        var transaction = await _osTestHelper.GenerateTransferTransaction();
        transaction.RefBlockNumber = chain.BestChainHeight + 1;
        var block = _osTestHelper.GenerateBlockWithTransactions(chain.BestChainHash, chain.BestChainHeight,
            new[] { transaction });

        var result = await _blockSyncValidationService.ValidateBlockBeforeAttachAsync(block);
        result.ShouldBeFalse();
    }

    private string GetEncodedPubKeyString()
    {
        var pk = CryptoHelper.GenerateKeyPair().PublicKey;
        var address = Address.FromPublicKey(pk);
        return address.ToBase58();
    }
}
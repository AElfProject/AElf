using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcPeerTests : GrpcNetworkTestBase
    {
        private IBlockchainService _blockchainService;
        private IAElfNetworkServer _networkServer;
        private IPeerPool _pool;
        private IPeer _grpcPeer;
        private IAccountService _acc;
        private ILocalEventBus _eventBus;

        public GrpcPeerTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkServer = GetRequiredService<IAElfNetworkServer>();
            _pool = GetRequiredService<IPeerPool>();
            _acc = GetRequiredService<IAccountService>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            
            _grpcPeer = CreateNewPeer();
            _pool.AddPeer(_grpcPeer);
        }

        public override void Dispose()
        {
            AsyncHelper.RunSync(()=> _networkServer.StopAsync(false));
        }

        [Fact]
        public async Task RequestBlockAsync_Success()
        {
            var block = await _grpcPeer.RequestBlockAsync(Hash.Generate());
            block.ShouldBeNull();

            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            block = await _grpcPeer.RequestBlockAsync(blockHeader.GetHash());
            block.ShouldNotBeNull();
        }

        [Fact]
        public async Task RequestBlockAsync_Failed()
        {
            _grpcPeer = CreateNewPeer("127.0.0.1:3000", false);
            _pool.AddPeer(_grpcPeer);
            
            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var block = await _grpcPeer.RequestBlockAsync(blockHeader.GetHash());
            
            block.ShouldBeNull();
        }

        [Fact]
        public async Task GetBlocksAsync_Success()
        {
            var chain = await _blockchainService.GetChainAsync();
            var genesisHash = chain.GenesisBlockHash;

            var blocks = await _grpcPeer.GetBlocksAsync(genesisHash, 5);
            blocks.Count.ShouldBe(5);
            blocks.Select(o =>o.Height).ShouldBe(new long[]{2, 3, 4, 5, 6});
        }

        [Fact]
        public async Task AnnounceAsync_Success()
        {
            AnnouncementReceivedEventData received = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(a =>
            {
                received = a;
                return Task.CompletedTask;
            });
            
            var header = new PeerNewBlockAnnouncement
            {
                BlockHeight = 100,
                BlockHash = Hash.Generate()
            };

            await _grpcPeer.AnnounceAsync(header);
            
            received.ShouldNotBeNull();
            received.Announce.BlockHeight.ShouldBe(100);
        }

        [Fact]
        public async Task SendTransactionAsync_Success()
        {
            TransactionsReceivedEvent received = null;
            _eventBus.Subscribe<TransactionsReceivedEvent>(t =>
            {
                received = t;
                return Task.CompletedTask;
            });
            
            var transaction = new Transaction
            {
                From = Address.Generate(),
                To = Address.Generate()
            };

            await _grpcPeer.SendTransactionAsync(transaction);
            
            received.ShouldNotBeNull();
            received.Transactions.Count().ShouldBe(1);
            received.Transactions.First().From.ShouldBe(transaction.From);
        }
        
        [Fact]
        public async Task DisconnectAsync_Success()
        {
            var peers = _pool.GetPeers();
            peers.Count.ShouldBe(2);

            await _grpcPeer.SendDisconnectAsync();
            peers = _pool.GetPeers();
            peers.Count.ShouldBe(1);
        }

        private GrpcPeer CreateNewPeer(string ipAddress = "127.0.0.1:2000", bool isValid = true)
        {
            var channel = new Channel(ipAddress, ChannelCredentials.Insecure);
            var publicKey = AsyncHelper.RunSync(() => _acc.GetPublicKeyAsync()).ToHex();
            PeerService.PeerServiceClient client = null; 
            if(isValid)
                client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
                {
                    metadata.Add(GrpcConsts.PubkeyMetadataKey, publicKey);
                    return metadata;
                }));
            else
                client = new PeerService.PeerServiceClient(channel);                
            
            return new GrpcPeer(channel, client, publicKey, ipAddress);
        }

        private async Task RestartNetworkServer()
        {
            await _networkServer.StopAsync();

            await _networkServer.StartAsync();
        }
    }
}
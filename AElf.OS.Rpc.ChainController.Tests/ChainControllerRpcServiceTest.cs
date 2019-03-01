using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Rpc.ChainController.Tests
{
    public class ChainControllerRpcServiceServerTest : RpcTestBase
    {
        public ILogger<ChainControllerRpcServiceServerTest> Logger { get; set; }
        private readonly IBlockchainService _blockchainService;
        private readonly IMinerService _minerService;
        private readonly ITxHub _txHub;
        
        private readonly int _chainId;
        private readonly ECKeyPair _keyPair;

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            Logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                     NullLogger<ChainControllerRpcServiceServerTest>.Instance;

            _blockchainService = GetRequiredService<IBlockchainService>();
            _txHub = GetRequiredService<ITxHub>();
            _minerService = GetRequiredService<IMinerService>();
            
            _chainId = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value.ChainId;
            
            _keyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public async Task Get_BlockHeight_Success()
        {
            await GenerateOneBlock();
            
            var response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            Logger.LogInformation(response.ToString());
            var height = (int) response["result"];
            height.ShouldBe(2);
        }

        private async Task<Block> GenerateOneBlock()
        {
            var preBlock = await _blockchainService.GetBestChainLastBlock(_chainId);

            var tx = await GenerateTransaction(preBlock.GetHash(), Address.BuildContractAddress(_chainId, 0),
                "DeploySmartContract", 2, File.ReadAllBytes(typeof(BasicContractZero).Assembly.Location));
            await _txHub.AddTransactionAsync(_chainId, tx);

            var block = await _minerService.MineAsync(_chainId, preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(4000));

            return block;
        }
        
        private async Task<Transaction> GenerateTransaction( Hash bestChainHash, Address contractAddress, string methodName, params object[]
            objects)
        {
            var tx = new Transaction
            {
                From = Address.FromPublicKey(_keyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = _blockchainService.GetBestChainLastBlock(_chainId).Result.Height,
                RefBlockPrefix = ByteString.CopyFrom(bestChainHash.DumpByteArray().Take(4).ToArray())
            };

            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }
    }
}
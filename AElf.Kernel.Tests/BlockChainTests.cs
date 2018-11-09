using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using Google.Protobuf;
using NServiceKit.Common.Extensions;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class BlockChainTests
    {
        private BlockChainTests_MockSetup _mock;
        private IExecutingService _executingService;

        public BlockChainTests(BlockChainTests_MockSetup mock, SimpleExecutingService executingService)
        {
            _mock = mock;
            _executingService = executingService;
        }

        [Fact(Skip = "Skip for now.")]
        public void StateRollbackTest()
        {
            var key =  new KeyPairGenerator().Generate();
            var addresses = Enumerable.Range(0, 10).Select(x => Address.FromString(x.ToString())).ToList();
            var txs = addresses.Select(x => _mock.GetInitializeTxn(x, 1)).ToList();

            var b1 = new Block()
            {
                Header = new BlockHeader()
                {
                    ChainId = _mock.ChainId1,
                    Index = _mock.BlockChain.GetCurrentBlockHeightAsync().Result + 1,
                    PreviousBlockHash = _mock.BlockChain.GetCurrentBlockHashAsync().Result,
                    P = ByteString.CopyFrom(key.GetEncodedPublicKey())
                },
                Body =  new BlockBody()
            };
            b1.Body.Transactions.AddRange(txs.Select(x => x.GetHash()));
            b1.Body.TransactionList.AddRange(txs);
            
            var disHash1 = b1.Header.GetDisambiguationHash();
            _executingService.ExecuteAsync(txs, _mock.ChainId1, CancellationToken.None, disHash1);
            
            _mock.BlockChain.AddBlocksAsync(new List<IBlock>() {b1});

            foreach (var addr in addresses)
            {
                Assert.Equal((ulong) 1, _mock.GetBalance(addr));
            }

            var tfrs = Enumerable.Range(0, 5)
                .Select(i => _mock.GetTransferTxn1(addresses[2 * i], addresses[2 * i + 1], 1)).ToList();

            var b2 = new Block()
            {
                Header = new BlockHeader()
                {
                    ChainId = _mock.ChainId1,
                    Index = _mock.BlockChain.GetCurrentBlockHeightAsync().Result + 1,
                    PreviousBlockHash = _mock.BlockChain.GetCurrentBlockHashAsync().Result,
                    P = ByteString.CopyFrom(key.GetEncodedPublicKey())
                },
                Body =  new BlockBody()
            };

            b2.Body.Transactions.AddRange(tfrs.Select(x => x.GetHash()));
            b2.Body.TransactionList.AddRange(tfrs);
            
            var disHash2 = b2.Header.GetDisambiguationHash();
            _executingService.ExecuteAsync(tfrs, _mock.ChainId1, CancellationToken.None, disHash2);

            _mock.BlockChain.AddBlocksAsync(new List<IBlock>() {b2});
            foreach (var i in Enumerable.Range(0, 5))
            {
                Assert.Equal((ulong) 0, _mock.GetBalance(addresses[2 * i]));
                Assert.Equal((ulong) 2, _mock.GetBalance(addresses[2 * i + 1]));
            }

            _mock.BlockChain.RollbackToHeight(2);

            foreach (var addr in addresses)
            {
                Assert.Equal((ulong) 1, _mock.GetBalance(addr));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Common;
using Google.Protobuf.Collections;
using Shouldly;

namespace AElf.Kernel.Types.Tests
{
    public class BlockTests
    {
        [Fact]
        public void BlockHeader_Test()
        {
            var blockHeader = new BlockHeader()
            {
                PreviousBlockHash = Hash.Generate(),
                ChainId = 1234,
                Height = 3,
            };
            var hash = blockHeader.GetHash();
            hash.ShouldNotBe(null);

            var hashByte = blockHeader.GetHashBytes();
            hashByte.ShouldNotBe(null);
        }

        [Fact]
        public void BlockBody_Test()
        {
            var blockHeader = new BlockHeader()
            {
                PreviousBlockHash = Hash.Generate(),
                ChainId = 1234,
                Height = 1,
            };
            var transactionItems = GenerateFakeTransactions(3);
            var blockBody = new BlockBody()
            {
                BlockHeader = blockHeader.GetHash(),
                TransactionList = { transactionItems.Item1},
                Transactions = { transactionItems.Item2},
                BinaryMerkleTree = {
                    Nodes = { Hash.Generate(), Hash.Generate() },
                    LeafCount = 2
                }
            };
            blockBody.TransactionsCount.ShouldBe(3);
            blockBody.BinaryMerkleTree.ShouldNotBe(null);
        }

        [Fact]
        public void BlockTest()
        {
            var block = CreateBlock(Hash.Generate(), 1234, 10);
            block.Height.ShouldBe(10u);
            block.BlockHashToHex.ShouldNotBe(string.Empty);

            var hash = block.GetHash();
            hash.ShouldNotBe(null);

            var hashBytes = block.GetHashBytes();
            hashBytes.ShouldNotBe(null);
            hashBytes.Length.ShouldBe(32);

            var serializeData = block.Serialize();
            serializeData.ShouldNotBe(null);
        }

        private Block CreateBlock(Hash preBlockHash, int chainId, long height)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Empty, null);

            var block = new Block(Hash.Generate());

            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Height = height;
            block.Header.MerkleTreeRootOfWorldState = Hash.Default;

            block.Body.BlockHeader = block.Header.GetHash();
            var transactionItems = GenerateFakeTransactions(3);
            block.Body.TransactionList.AddRange(transactionItems.Item1);
            block.Body.Transactions.AddRange(transactionItems.Item2);

            return block;
        }

        private (List<Transaction>, List<Hash>) GenerateFakeTransactions(int count)
        {
            var transactions = new List<Transaction>();
            var transactionHashes = new List<Hash>();
            for (int i = 0; i < count; i++)
            {
               var transaction = new Transaction()
               {
                   From = Address.Generate(),
                   To = Address.Generate(),
                   MethodName = $"Test{i}",
                   Params = ByteString.Empty
               };
               var hash = transaction.GetHash();

               transactions.Add(transaction);
               transactionHashes.Add(hash);
            }

            return (transactions, transactionHashes);
        }
    }
}
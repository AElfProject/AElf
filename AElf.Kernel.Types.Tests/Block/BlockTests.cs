﻿using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class BlockTests
    {
        private Block CreateBlock(Hash preBlockHash, int chainId, long height)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Empty, null);

            var block = new Block(Hash.Generate());

            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Height = height;
            block.Header.MerkleTreeRootOfWorldState = Hash.Empty;

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
            for (var i = 0; i < count; i++)
            {
                var transaction = new Transaction
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

        [Fact]
        public void BlockBody_Test()
        {
            var blockHeader = new BlockHeader
            {
                PreviousBlockHash = Hash.Generate(),
                ChainId = 1234,
                Height = 1
            };
            var transactionItems = GenerateFakeTransactions(3);
            var blockBody = new BlockBody
            {
                BlockHeader = blockHeader.GetHash(),
                TransactionList = {transactionItems.Item1},
                Transactions = {transactionItems.Item2},
                BinaryMerkleTree =
                {
                    Nodes = {Hash.Generate(), Hash.Generate()},
                    LeafCount = 2
                }
            };
            blockBody.TransactionsCount.ShouldBe(3);
            blockBody.BinaryMerkleTree.ShouldNotBe(null);
        }

        [Fact]
        public void BlockHeader_Test()
        {
            var blockHeader = new BlockHeader
            {
                PreviousBlockHash = Hash.Generate(),
                ChainId = 1234,
                Height = 3
            };
            var hash = blockHeader.GetHash();
            hash.ShouldNotBe(null);

            var hashByte = blockHeader.GetHashBytes();
            hashByte.ShouldNotBe(null);
        }

        [Fact]
        public void BlockTest()
        {
            var block = CreateBlock(Hash.Generate(), 1234, 10);
            block.Height.ShouldBe(10u);

            var hash = block.GetHash();
            hash.ShouldNotBe(null);

            var hashBytes = block.GetHashBytes();
            hashBytes.ShouldNotBe(null);
            hashBytes.Length.ShouldBe(32);

            var serializeData = block.Serialize();
            serializeData.ShouldNotBe(null);
        }

        [Fact]
        public void Get_BlockHash()
        {
            var blockHeader = new BlockHeader
            {
                ChainId = 2111,
                Height = 10,
                PreviousBlockHash = Hash.Generate(),
                MerkleTreeRootOfTransactions = Hash.Generate(),
                MerkleTreeRootOfWorldState = Hash.Generate(),
                Bloom = ByteString.Empty,
                BlockExtraDatas = {ByteString.CopyFromUtf8("test")}
            };
            var hash = blockHeader.GetHash();
            hash.ShouldNotBeNull();

            var hashBytes = blockHeader.GetHashBytes();
            hashBytes.Length.ShouldBe(32);

            var hash1 = Hash.LoadByteArray(hashBytes);
            hash.ShouldBe(hash1);
        }
    }
}
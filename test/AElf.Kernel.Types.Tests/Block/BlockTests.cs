using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Types;
using Google.Protobuf;
using Xunit;
using Shouldly;

namespace AElf.Kernel.Types.Tests
{
    public class BlockTests
    {
        [Fact]
        public void BlockHeader_Test()
        {
            var blockHeader = GenerateBlockHeader();
            var hash = blockHeader.GetHash();
            hash.ShouldNotBe(null);

            var hashByte = blockHeader.GetHashBytes();
            hashByte.ShouldNotBe(null);

            //exception cases
            blockHeader = GenerateBlockHeader();
            blockHeader.ChainId = -12;
            Should.Throw<InvalidOperationException>(() => { blockHeader.GetHash(); });

            blockHeader = GenerateBlockHeader();
            blockHeader.SignerPubkey = ByteString.Empty;
            Should.Throw<InvalidOperationException>(() => { blockHeader.GetHash(); });

            blockHeader = GenerateBlockHeader();
            blockHeader.PreviousBlockHash = null;
            Should.Throw<InvalidOperationException>(() => { blockHeader.GetHash(); });

            blockHeader = GenerateBlockHeader();
            blockHeader.MerkleTreeRootOfTransactionStatus = null;
            Should.Throw<InvalidOperationException>(() => { blockHeader.GetHash(); });

            blockHeader = GenerateBlockHeader();
            blockHeader.Time = null;
            Should.Throw<InvalidOperationException>(() => { blockHeader.GetHash(); });
        }

        [Fact]
        public void BlockBody_Test()
        {
            var transactionItems = GenerateFakeTransactions(3);
            var blockBody = new BlockBody()
            {
                TransactionIds = {transactionItems.Item2}
            };
            blockBody.TransactionsCount.ShouldBe(3);
        }

        [Fact]
        public void CalculateBodyHash_Test()
        {
            //transaction count == 0
            var blockBody = new BlockBody();
            Should.Throw<InvalidOperationException>(() => blockBody.GetHash());

            blockBody = new BlockBody
            {
                TransactionIds =
                {
                    new[]
                    {
                        HashHelper.ComputeFrom("tx1"),
                        HashHelper.ComputeFrom("tx2"),
                        HashHelper.ComputeFrom("tx3"),
                    }
                }
            };
            var hash = blockBody.GetHash();
            hash.ShouldNotBeNull();
        }

        [Fact]
        public void BlockIndex_Test()
        {
            var blockIndex = new BlockIndex();
            blockIndex.BlockHash = Hash.Empty;
            blockIndex.BlockHeight = 1L;

            var blockIndex1 = new BlockIndex(Hash.Empty, 1L);

            blockIndex.ToString().ShouldBe(blockIndex1.ToString());
            blockIndex.ToString().Contains(Hash.Empty.ToString()).ShouldBeTrue();
            blockIndex.ToString().Contains("1").ShouldBeTrue();
            
            blockIndex.ToDiagnosticString().ShouldBe($"[{blockIndex.BlockHash}: {blockIndex.BlockHeight}]");
        }

        [Fact]
        public void Block_Test()
        {
            var block = CreateBlock(HashHelper.ComputeFrom("hash"), 1234, 10);
            block.Height.ShouldBe(10u);

            var hash = block.GetHash();
            hash.ShouldNotBe(null);
        }

        [Fact]
        public void Get_BlockHash_Test()
        {
            var blockHeader = GenerateBlockHeader();
            var hash = blockHeader.GetHash();
            hash.ShouldNotBeNull();

            var hashBytes = blockHeader.GetHashBytes();
            hashBytes.Length.ShouldBe(32);

            var hash1 = Hash.LoadFromByteArray(hashBytes);
            hash.ShouldBe(hash1);
        }
        [Fact]
        public void GetHashWithoutCache_Test()
        { 
            var blockHeader = GenerateBlockHeader();
            var hash = blockHeader.GetHashWithoutCache();
            hash.ShouldNotBe(null);

            var blockHeader1 = GenerateBlockHeader();
            blockHeader1.Signature =
                ByteStringHelper.FromHexString("782330156f8c9403758ed30270a3e2d59e50b8f04c6779d819b72eee02addb13");
            var  hash1=blockHeader1.GetHash();
            hash1.ShouldNotBe(null);
            
            var block = CreateBlock(HashHelper.ComputeFrom("hash"), 123, 10);
            block.Height.ShouldBe(10u);
            var hash2 = block.GetHashWithoutCache();
            hash2.ShouldNotBe(null);
            
            var blockHeader3 = GenerateBlockHeader();
            blockHeader3.Height = 0;
            Should.Throw<InvalidOperationException>(() => { blockHeader3.GetHash(); });
        }

        [Fact]
        public void BlockIndex_ToDiagnosticString_Test()
        {
            var hash = HashHelper.ComputeFrom("test");
            var height = 10;
            var blockIndex = new BlockIndex
            {
                BlockHash = hash,
                BlockHeight = height
            };
            blockIndex.ToDiagnosticString().ShouldBe($"[{hash}: {height}]");
        }

        private Block CreateBlock(Hash preBlockHash, int chainId, long height)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Empty, null);

            var block = new Block(HashHelper.ComputeFrom("hash1"));

            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = TimestampHelper.GetUtcNow();
            block.Header.Height = height;
            var transactionItems = GenerateFakeTransactions(3);
            block.Body.TransactionIds.AddRange(transactionItems.Item2);

            block.Header.MerkleTreeRootOfTransactions =
                BinaryMerkleTree.FromLeafNodes(block.Body.TransactionIds).Root;
            block.Header.MerkleTreeRootOfWorldState = Hash.Empty;
            block.Header.MerkleTreeRootOfTransactionStatus = Hash.Empty;
            block.Header.SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey");

            return block;
        }

        private (List<Transaction>, List<Hash>) GenerateFakeTransactions(int count)
        {
            var transactions = new List<Transaction>();
            var transactionIds = new List<Hash>();
            for (int i = 0; i < count; i++)
            {
                var transaction = new Transaction()
                {
                    From = Address.FromBase58("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM"),
                    To = Address.FromBase58("2vNDCj1WjNLAXm3VnEeGGRMw3Aab4amVSEaYmCyxQKjNhLhfL7"),
                    MethodName = $"Test{i}",
                    Params = ByteString.Empty
                };
                var hash = transaction.GetHash();

                transactions.Add(transaction);
                transactionIds.Add(hash);
            }

            return (transactions, transactionIds);
        }

        private BlockHeader GenerateBlockHeader()
        {
            return new BlockHeader
            {
                ChainId = 1234,
                Height = 10,
                PreviousBlockHash = HashHelper.ComputeFrom("hash3"),
                MerkleTreeRootOfTransactions = Hash.Empty,
                MerkleTreeRootOfWorldState = Hash.Empty,
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactionStatus = Hash.Empty,
                SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey")
            };
        }
    }
}
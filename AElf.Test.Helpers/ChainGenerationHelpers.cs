using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Helpers;
using Google.Protobuf;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public static class ChainGenerationHelpers
    {
        /// <summary>
        /// Generates <see cref="count"/> random miners.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<ECKeyPair> GetRandomMiners(int count = 3)
        {
            Assert.True(count > 0);
            
            List<ECKeyPair> keyPairs = new List<ECKeyPair>(count);
            
            for (int i = 0; i < count; i++)
                keyPairs.Add(CryptoHelpers.GenerateKeyPair());
            
            return keyPairs;
        }

//        public static List<string> ToPubKeyStrings(this List<ECKeyPair> keyPairs)
//        {
//            List<string> pubKeyStrings = new List<string>(keyPairs.Count);
//            
//            foreach (var key in keyPairs)
//                pubKeyStrings.Add(key.PublicKey.ToHex());
//
//            return pubKeyStrings;
//        }
        
        /// <summary>
        /// Get random chain Id
        /// </summary>
        /// <returns></returns>
        public static int GetRandomChainId()
        {
            var random = new Random();
            var chainId = ChainHelpers.GetChainId(random.Next(195112, 11316496));
            return chainId;
        }
        
        /// <summary>
        /// Builds the genesis block with AElfs builder.
        /// </summary>
        /// <returns></returns>
        public static Block GetGenesisBlock()
        {
            var chainId = 2111;
            var builder = new GenesisBlockBuilder().Build(chainId);
            return builder.Block;
        }
        
        /// <summary>
        /// Given a block, will generate the next, only the height and
        /// the previous will not be random.
        /// </summary>
        /// <param name="previous">The block to build upon on.</param>
        /// <returns>The new block</returns>
        public static Block BuildNext(Block previous, string producer = null)
        {
            Assert.NotNull(previous);

            return new Block
            {
                Header = new BlockHeader
                {
                    Height = previous.Header.Height + 1,
                    MerkleTreeRootOfTransactions = Hash.Generate(),
                    ChainId = ChainHelpers.GetChainId(123),
                    PreviousBlockHash = previous.GetHash(),
                    MerkleTreeRootOfWorldState = Hash.Generate(),
                    P = producer == null ? ByteString.Empty : ByteString.CopyFromUtf8(producer)
                }
            };
        }

        /// <summary>
        /// Will create a chain from start with <see cref="count"/> extra blocks.
        /// Total block count will be <see cref="count"/>+1. 
        /// </summary>
        /// <param name="start">The start block, if null will create a genesis block.</param>
        /// <param name="count">The amount of extra blocks to create</param>
        /// <returns>return the generated chain</returns>
        public static List<Block> GenerateChain(int count, Block start = null)
        {
            Assert.True(count > 0);

            List<Block> blocks = new List<Block>();

            Block current = start ?? GetGenesisBlock();
            blocks.Add(current);

            for (int i = 0; i < count; i++)
            {
                current = BuildNext(current);
                blocks.Add(current);
            }

            return blocks;
        }
    }
}
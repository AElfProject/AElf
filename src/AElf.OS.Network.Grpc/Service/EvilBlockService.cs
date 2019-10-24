using System;
using System.Collections.Generic;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.Network.Grpc
{
    public class EvilBlockService
    {
        private Random EvilRandom;
        private Timestamp EvilTime;
        private bool EvilStatus;
        public ILogger<GrpcServerService> Logger { get; set; }

        public EvilBlockService()
        {
            EvilRandom = new Random(Guid.NewGuid().GetHashCode());
            EvilTime = TimestampHelper.GetUtcNow();
            EvilStatus = false;
            Logger = NullLogger<GrpcServerService>.Instance;
        }

        public BlockReply EvilBlock(BlockReply original, ServerCallContext context)
        {
            if (original.Block == null) return original;
            if (!EvilOrNot(out var number)) return original;

            Logger.LogWarning($"### Evil block to {context.GetPeerInfo()}");
            switch (number % 3)
            {
                case 0:
                    original.Block.Header.SignerPubkey = ByteString.CopyFromUtf8("fake public key");
                    break;
                case 1:
                    original.Block.Header.Signature = ByteString.CopyFromUtf8("fake signature");
                    break;
                case 2:
                    var block = new BlockWithTransactions
                    {
                        Header = original.Block.Header,
                        Transactions = {GenerateInvalidTransaction(5).ToArray()}
                    };
                    original.Block = block;
                    break;
            }

            return original;
        }

        public BlockList EvilBlockList(BlockList original, ServerCallContext context)
        {
            if (original.Equals(new BlockList())) return original;
            if (!EvilOrNot(out var number)) return original;
            
            Logger.LogWarning($"### Evil blocks to {context.GetPeerInfo()}");
            var list = new BlockList();
            var rdBlockNo = EvilRandom.Next(0, original.Blocks.Count);
            switch (number % 3)
            {
                case 0:
                    original.Blocks[rdBlockNo].Header.Bloom = ByteString.CopyFromUtf8("invalid bloom");
                    original.Blocks[rdBlockNo].Header.Height = number;
                    break;
                case 1:
                    original.Blocks[rdBlockNo].Header.SignerPubkey = ByteString.CopyFromUtf8("invalid signature");
                    break;
                case 2:
                    original.Blocks[rdBlockNo].Header.PreviousBlockHash = Hash.FromString("invalid hash");
                    break;
            }

            return list;
        }

        private bool EvilOrNot(out int number)
        {
            number = 0;
            if (EvilStatus)
            {
                //recover
                if ((TimestampHelper.GetUtcNow() - EvilTime).Seconds >= 360)
                {
                    EvilStatus = false;
                }

                return false;
            }

            number = EvilRandom.Next(1, 101);
            if (number >= 5) return false;

            EvilStatus = true;
            EvilTime = TimestampHelper.GetUtcNow();
            return true;
        }

        private List<Transaction> GenerateInvalidTransaction(int number)
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < number; i++)
            {
                var keyPair = CryptoHelper.GenerateKeyPair();
                var transaction = new Transaction
                {
                    From = Address.FromPublicKey(keyPair.PublicKey),
                    To = AddressHelper.Base58StringToAddress("25CecrU94dmMdbhC3LWMKxtoaL4Wv8PChGvVJM6PxkHAyvXEhB"),
                    MethodName = "Transfer",
                    Params = ByteString.CopyFromUtf8($"invalid parameter - {Guid.NewGuid()}"),
                };
                transaction.Signature =
                    ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.ToByteArray()));
                transactions.Add(transaction);
            }

            return transactions;
        }
    }
}
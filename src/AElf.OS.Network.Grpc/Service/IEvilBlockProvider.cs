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
    public interface IEvilBlockProvider
    {
        BlockReply EvilBlock(BlockReply original, ServerCallContext context);
        BlockList EvilBlockList(BlockList original, ServerCallContext context);
    }

    public class EvilBlockProvider : IEvilBlockProvider
    {
        private Timestamp _evilTime;
        private bool _evilStatus;
        public ILogger<GrpcServerService> Logger { get; set; }

        public EvilBlockProvider()
        {
            _evilTime = TimestampHelper.GetUtcNow();
            _evilStatus = false;

            Logger = NullLogger<GrpcServerService>.Instance;
        }

        public BlockReply EvilBlock(BlockReply original, ServerCallContext context)
        {
            if (original.Block == null) return original;
            if (!EvilOrNot(4, out var number)) return original;

            Logger.LogWarning($"### Evil block to {context.GetPeerInfo()} with case: {number % 4}");
            switch (number % 4)
            {
                case 0:
                    original.Block.Header.SignerPubkey = ByteString.CopyFromUtf8("fake public key");
                    break;
                case 1:
                    original.Block.Header.Signature = ByteString.CopyFromUtf8("fake signature");
                    break;
                case 2:
                    original.Block.Header.PreviousBlockHash = Hash.FromString("invalid one");
                    break;
                case 3:
                    original.Block = new BlockWithTransactions
                    {
                        Header = original.Block.Header,
                        Transactions = {GenerateInvalidTransaction(5)}
                    };
                    break;
            }

            return original;
        }

        public BlockList EvilBlockList(BlockList original, ServerCallContext context)
        {
            if (original.Equals(new BlockList())) return original;
            if (!EvilOrNot(5, out var number)) return original;

            Logger.LogWarning($"### Evil blocks to {context.GetPeerInfo()} with case: {number % 3}");
            var rdBlockNo = new Random(DateTime.Now.GetHashCode()).Next(0, original.Blocks.Count);
            switch (number % 4)
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
                case 3:
                    original.Blocks[rdBlockNo] = new BlockWithTransactions
                    {
                        Header = original.Blocks[rdBlockNo].Header,
                        Transactions = {GenerateInvalidTransaction(3)}
                    };
                    break;
            }

            return original;
        }

        private bool EvilOrNot(int fakePercent, out int number)
        {
            number = 0;
            if (_evilStatus)
            {
                //recover
                if ((TimestampHelper.GetUtcNow() - _evilTime).Seconds >= 360)
                {
                    _evilStatus = false;
                }

                return false;
            }

            var rd = new Random(DateTime.Now.Millisecond);
            number = rd.Next(1, 101);
            if (number >= fakePercent) return false;

            _evilStatus = true;
            _evilTime = TimestampHelper.GetUtcNow();
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
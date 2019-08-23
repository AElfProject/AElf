using System;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Sdk.CSharp.Tests
{
    public class SerializationHelperTests
    {
        [Fact]
        public void Serialization_Bool_Test()
        {
            var boolValue = true;
            var byteArray = SerializationHelper.Serialize(boolValue);
            var serializeValue = SerializationHelper.Deserialize<bool>(byteArray);
            boolValue.ShouldBe(serializeValue);

            var boolValue1 = false;
            var byteArray1 = SerializationHelper.Serialize(boolValue1);
            var serializeValue1 = SerializationHelper.Deserialize<bool>(byteArray1);
            boolValue1.ShouldBe(serializeValue1);
        }

        [Fact]
        public void Serialization_Int_Test()
        {
            var intValue = new Random().Next();
            var byteArray = SerializationHelper.Serialize(intValue);
            var serializeValue = SerializationHelper.Deserialize<int>(byteArray);
            intValue.ShouldBe(serializeValue);
        }

        [Fact]
        public void Serialization_UInt_Test()
        {
            var uintValue = Convert.ToUInt32(new Random().Next());
            var byteArray = SerializationHelper.Serialize(uintValue);
            var serializeValue = SerializationHelper.Deserialize<uint>(byteArray);
            uintValue.ShouldBe(serializeValue);
        }

        [Fact]
        public void Serialization_Long_Test()
        {
            var longArray = new[] {long.MinValue, -10054, -100, -50, 0, 50, 100, 10005, long.MaxValue};
            foreach (var longValue in longArray)
            {
                var byteArray = SerializationHelper.Serialize(longValue);
                var serializeValue = SerializationHelper.Deserialize<long>(byteArray);
                longValue.ShouldBe(serializeValue);
            }
        }

        [Fact]
        public void Serialization_ULong_Test()
        {
            var ulongArray = new ulong[] { ulong.MinValue, 50, 100, 1004, ulong.MaxValue };
            foreach (var longValue in ulongArray)
            {
                var byteArray = SerializationHelper.Serialize(longValue);
                var serializeValue = SerializationHelper.Deserialize<ulong>(byteArray);
                longValue.ShouldBe(serializeValue);
            }
        }

        [Fact]
        public void Serialization_UnSupported_Type()
        {
            //Serialize
            var byteString = ByteString.CopyFromUtf8("ByteString shold not supported.");
            Should.Throw<InvalidOperationException>(() => SerializationHelper.Serialize(byteString));

            //Deserialize
            var byteArray = byteString.ToByteArray();
            Should.Throw<InvalidOperationException>(() => SerializationHelper.Deserialize<ByteString>(byteArray));
        }

        [Fact]
        public void Serialization_ByteArray_Test()
        {
            //Hash test
            var hash = Hash.FromString("hash");
            var hashArray = SerializationHelper.Serialize(hash);
            var hash1 = SerializationHelper.Deserialize<Hash>(hashArray);
            hash.ShouldBe(hash1);

            //Address test
            var address = SampleAddress.AddressList[0];
            var addressArray = SerializationHelper.Serialize(address);
            var address1 = SerializationHelper.Deserialize<Address>(addressArray);
            address.ShouldBe(address1);

            //Transaction test
            var transaction = new Transaction
            {
                From = SampleAddress.AddressList[1],
                To = SampleAddress.AddressList[2],
                Params = ByteString.CopyFromUtf8("test"),
                MethodName = "TestMethod",
                RefBlockNumber = 1,
                RefBlockPrefix = ByteString.Empty
            };
            var transactionArray = SerializationHelper.Serialize(transaction);
            var transaction1 = SerializationHelper.Deserialize<Transaction>(transactionArray);
            transaction.ShouldBe(transaction1);

            //Block header test
            var header = new BlockHeader
            {
                ChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                Height = Constants.GenesisBlockHeight,
                Bloom = ByteString.CopyFromUtf8("bloom"),
                PreviousBlockHash = Hash.FromString("PreviousBlockHash"),
                MerkleTreeRootOfTransactions = Hash.FromString("MerkleTreeRootOfTransactions"),
                MerkleTreeRootOfWorldState = Hash.FromString("MerkleTreeRootOfWorldState"),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactionStatus = Hash.FromString("MerkleTreeRootOfTransactionStatus")
            };
            var headerArray = SerializationHelper.Serialize(header);
            var header1 = SerializationHelper.Deserialize<BlockHeader>(headerArray);
            header.ShouldBe(header1);

            //Block body test
            var body = new BlockBody
            {
                BlockHeader = header.GetHash(),
                TransactionIds = { transaction.GetHash() }
            };
            var bodyArray = SerializationHelper.Serialize(body);
            var body1 = SerializationHelper.Deserialize<BlockBody>(bodyArray);
            body.ShouldBe(body1);

            //Block test
            var block = new Block
            {
                Body = body,
                Header = header,
            };
            var blockArray = SerializationHelper.Serialize(block);
            var block1 = SerializationHelper.Deserialize<Block>(blockArray);
            block.ShouldBe(block1);
        }
    }
}
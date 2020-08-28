using System;
using System.Linq;
using System.Text;
using AElf.Cryptography;
using Xunit;
using Google.Protobuf;
using Shouldly;

namespace AElf.Types.Tests.Extensions
{
    public class ExtensionTests
    {
        [Fact]
        public void Number_Extensions_Methods_Test()
        {
            //ulong
            var uNumber = (ulong)10;
            var byteArray = uNumber.ToBytes();
            byteArray.ShouldNotBe(null);

            //int
            var iNumber = 10;
            var byteArray1 = iNumber.ToBytes();
            byteArray1.ShouldNotBe(null);

            //hash
            var hash = HashHelper.ComputeFrom(iNumber);
            hash.ShouldNotBe(null);
        }

        [Fact]
        public void Byte_Extensions_ToPlainBase58_Test()
        {
            var emptyByteString = ByteString.Empty;
            emptyByteString.ToPlainBase58().ShouldBe(string.Empty);
            
            var byteString = ByteString.CopyFromUtf8("5ta1yvi2dFEs4V7YLPgwkbnn816xVUvwWyTHPHcfxMVLrLB");
            byteString.ToPlainBase58().ShouldBe("SmUQnCq4Ffvy8UeR9EEV9DhNVcNaLhGpqFTDZfzdebANJAgngqe8RfT1sqPPqJQ9");

            var bytes = new byte[] {0, 0, 0};
            byteString = ByteString.CopyFrom(bytes);
            byteString.ToPlainBase58().ShouldBe("111");
        }
        [Fact]
        public void ByteStringExtension_toHex_Test()
        { 
            var hash=HashHelper.ComputeFrom("hash");
            var result=   hash.Value.ToHex(true);
            result.ShouldNotBe(null);
            result.IndexOf("0x").ShouldBe(0);
                
            var result1=   hash.Value.ToHex();
            result1.ShouldNotBe(null);
            result1.IndexOf("0x").ShouldBe(-1);
        }

        [Fact]
        public void ByteStringExtension_IsNullOrEmpty_Test()
        {
            var byteString1 = ByteString.CopyFrom(new byte[] { });
            var result=   byteString1.IsNullOrEmpty();
            result.ShouldBe(true);
            
            var hash=HashHelper.ComputeFrom("hash");
            var result1= hash.Value.IsNullOrEmpty();
            result1.ShouldBe(false);
        }

        [Fact]
        public void ByteExtensions_ToHex_Test()
        {
            var hash=HashHelper.ComputeFrom("hash");
            var result = hash.ToByteArray().ToHex(true);
            result.ShouldNotBe(null);
            result.IndexOf("0x").ShouldBe(0);
                
            var result1 = hash.ToByteArray().ToHex(false);
            result1.ShouldNotBe(null);
            result1.IndexOf("0x").ShouldBe(-1);
        }

        [Fact]
        public void ByteExtensions_Find_Test()
        {
            var bytes1 = new byte[] {5, 2, 1, 8,9,0,5,2,0};
            var bytes2 = new byte[] {4, 1};
            var bytes3 = new byte[] {5, 7};
            var bytes4 = new byte[] {2, 3};
            var bytes5 = new byte[] {5, 2, 0};
            var result2 = bytes1.Find(bytes2);
            result2.ShouldBe(-1);
            var result3 = bytes1.Find(bytes3);
            result3.ShouldBe(-1);
            var result4 = bytes1.Find(bytes4);
            result4.ShouldBe(-1);
            var result5 = bytes1.Find(bytes5);
            result5.ShouldBe(6);
        }
        [Fact]
        public void IMessageExtensions_ToBytesValue_Test()
        {
             var message=new Transaction();
             var result= message.ToBytesValue();
             result.Value.ShouldBeEmpty();
        }
        [Fact]
        public void StringExtensions_GetBytes_Test()
        {
            BlockHelper.GetRefBlockPrefix(Hash.Empty);
            var string1 = "string";
           var result= string1.GetBytes();
           var expected = Encoding.UTF8.GetBytes(string1);
           result.ShouldBe(expected);
        }

        [Fact]
        public void MerklePathExtensions_ComputeRootWithLeafNode_Test()
        {
            // left node
            {
                var merkleLeftNode = new MerklePathNode
                {
                    Hash = HashHelper.ComputeFrom("node1"),
                    IsLeftChildNode = true
                };
            
                var nodePath = new MerklePath
                {
                    MerklePathNodes = { merkleLeftNode }
                };

                var hash = HashHelper.ComputeFrom("new");

                var calculateHash = nodePath.ComputeRootWithLeafNode(hash);
                var targetHash = HashHelper.ConcatAndCompute(merkleLeftNode.Hash, hash);
                calculateHash.ShouldBe(targetHash);
            }
            
            // right node
            {
                var merkleLeftNode = new MerklePathNode
                {
                    Hash = HashHelper.ComputeFrom("node1"),
                    IsLeftChildNode = false
                };
            
                var nodePath = new MerklePath
                {
                    MerklePathNodes = { merkleLeftNode }
                };

                var hash = HashHelper.ComputeFrom("new");

                var calculateHash = nodePath.ComputeRootWithLeafNode(hash);
                var targetHash = HashHelper.ConcatAndCompute(hash, merkleLeftNode.Hash);
                calculateHash.ShouldBe(targetHash);
            }
        }

        [Fact]
        public void NumericExtensions_ToBytes_For_long()
        {
            long number = -2;
            var bigEndianBytes = number.ToBytes(true);
            ((int)bigEndianBytes.Last()).ShouldBe(254);
            var numberFromBigEndianBytes = BitConverter.ToInt64(BitConverter.IsLittleEndian ? bigEndianBytes.Reverse().ToArray() : bigEndianBytes);
            numberFromBigEndianBytes.ShouldBe(number);

            var littleEndianBytes = number.ToBytes(false);
            ((int)littleEndianBytes.Last()).ShouldBe(255);
            numberFromBigEndianBytes = BitConverter.ToInt64(BitConverter.IsLittleEndian ? littleEndianBytes: littleEndianBytes.Reverse().ToArray());
            numberFromBigEndianBytes.ShouldBe(number);
        }
        
        [Fact]
        public void NumericExtensions_ToBytes_For_ulong()
        {
            ulong number = ulong.MaxValue - 1;
            var bigEndianBytes = number.ToBytes(true);
            ((int)bigEndianBytes.Last()).ShouldBe(254);
            var numberFromBigEndianBytes = BitConverter.ToUInt64(BitConverter.IsLittleEndian ? bigEndianBytes.Reverse().ToArray() : bigEndianBytes);
            numberFromBigEndianBytes.ShouldBe(number);
            
            var littleEndianBytes = number.ToBytes(false);
            ((int)littleEndianBytes.Last()).ShouldBe(255);
            numberFromBigEndianBytes = BitConverter.ToUInt64(BitConverter.IsLittleEndian ? littleEndianBytes: littleEndianBytes.Reverse().ToArray());
            numberFromBigEndianBytes.ShouldBe(number);
        }
        
        [Fact]
        public void NumericExtensions_ToBytes_For_int()
        {
            int number = -2;
            var bigEndianBytes = number.ToBytes(true);
            ((int)bigEndianBytes.Last()).ShouldBe(254);
            var numberFromBigEndianBytes = BitConverter.ToInt32(BitConverter.IsLittleEndian ? bigEndianBytes.Reverse().ToArray() : bigEndianBytes);
            numberFromBigEndianBytes.ShouldBe(number);

            var littleEndianBytes = number.ToBytes(false);
            ((int)littleEndianBytes.Last()).ShouldBe(255);
            numberFromBigEndianBytes = BitConverter.ToInt32(BitConverter.IsLittleEndian ? littleEndianBytes: littleEndianBytes.Reverse().ToArray());
            numberFromBigEndianBytes.ShouldBe(number);
        }
        
        [Fact]
        public void NumericExtensions_ToBytes_For_uint()
        {
            uint number = uint.MaxValue - 1;
            var bigEndianBytes = number.ToBytes(true);
            ((int)bigEndianBytes.Last()).ShouldBe(254);
            var numberFromBigEndianBytes = BitConverter.ToUInt32(BitConverter.IsLittleEndian ? bigEndianBytes.Reverse().ToArray() : bigEndianBytes);
            numberFromBigEndianBytes.ShouldBe(number);
            
            var littleEndianBytes = number.ToBytes(false);
            ((int)littleEndianBytes.Last()).ShouldBe(255);
            numberFromBigEndianBytes = BitConverter.ToUInt32(BitConverter.IsLittleEndian ? littleEndianBytes: littleEndianBytes.Reverse().ToArray());
            numberFromBigEndianBytes.ShouldBe(number);
        }

        [Fact]
        public void StateKeyExtensions_ToStateKey_Test()
        {
            var statePath = new StatePath
            {
                Parts = { "1", "2", "3"}
            };
            
            var privateKey = ByteArrayHelper.HexStringToByteArray("5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f");
            var keyPair = CryptoHelper.FromPrivateKey(privateKey);
            var address = Address.FromPublicKey(keyPair.PublicKey);
            var addressBase58String = address.ToBase58();
            var targetKeyString = string.Join("/", new[] {addressBase58String}.Concat(statePath.Parts));
            var stateKey1 = statePath.ToStateKey(address);
            var scopedStatePath = new ScopedStatePath
            {
                Path = statePath,
                Address = address
            };
            var stateKey2 = scopedStatePath.ToStateKey();
            
            stateKey1.ShouldBe(targetKeyString);
            stateKey2.ShouldBe(stateKey1);
        }
    }
}
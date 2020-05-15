using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Types;
using Shouldly;

namespace TokenSwapContract.Tests
{
    public class TokenLocker
    {
        private readonly int _amountRangeSizeInByte;
        private List<Hash> _leafHashList = new List<Hash>();
        private BinaryMerkleTree _binaryMerkleTree;
        public Hash MerkleTreeRoot { get; private set; }

        public TokenLocker(int amountRangeSizeInByte)
        {
            _amountRangeSizeInByte = amountRangeSizeInByte;
        }

        public Hash Lock(Address address, int amount, bool isBidEndian, int lockId)
        {
            _amountRangeSizeInByte.ShouldBe(4);
            var index = _leafHashList.Count;
            GenerateNewLeaf(amount.ToBytes(isBidEndian), address, lockId);
            return HashHelper.ComputeFrom(lockId);
        }

        public Hash Lock(Address address, long amount, bool isBidEndian)
        {
            _amountRangeSizeInByte.ShouldBe(8);
            var index = _leafHashList.Count;
            GenerateNewLeaf(amount.ToBytes(isBidEndian), address);
            return HashHelper.ComputeFrom(index);
        }
        
        public Hash Lock(Address address, decimal amount, bool isBidEndian)
        {
            _amountRangeSizeInByte.ShouldBe(16);
            var index = _leafHashList.Count;
            var amountBytes = new List<byte>();
            var amountInIntegers =
                isBidEndian ? decimal.GetBits(amount).Reverse().ToArray() : decimal.GetBits(amount).ToArray();
            amountInIntegers.Aggregate(amountBytes, (cur, i) =>
            {
                cur.AddRange(i.ToBytes(isBidEndian));
                return cur;
            });
            GenerateNewLeaf(amountBytes.ToArray(), address);
            return HashHelper.ComputeFrom(index);
        }

        public void GenerateMerkleTree()
        {
            _binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(_leafHashList);
            MerkleTreeRoot = _binaryMerkleTree.Root;
            _leafHashList = new List<Hash>();
        }

        public MerklePath GetMerklePath(int index)
        {
            return _binaryMerkleTree.GenerateMerklePath(index);
        }

        private void GenerateNewLeaf(byte[] amountData, Address address)
        {
            _leafHashList.Add(ConcatLeafData(amountData, address.ToBase58(), _leafHashList.Count()));   
        }
        
        private Hash ConcatLeafData(byte[] amountData, string addressInString, int id)
        {
            return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(amountData), HashHelper.ComputeFrom(addressInString),
                HashHelper.ComputeFrom(id));
        }
        
        private void GenerateNewLeaf(byte[] amountData, Address address, int lockId)
        {
            _leafHashList.Add(ConcatLeafData(amountData, address.ToBase58(), lockId));
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moq;
using QuickGraph;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionSchedulerTest
    {
        private const int HASH_COUNT = 5;

        #region Helper Functions

        private ITransaction GetTransactionFromByte(byte b)
        {
            Mock<IHash<ITransaction>> hash = new Mock<IHash<ITransaction>>();
            hash.Setup(h => h.GetHashBytes()).Returns(new byte[]{b});
            hash.Setup(t => t.Equals(It.IsAny<IHash>())).Returns<IHash>(t => t?.GetHashBytes()[0] == t.GetHashBytes()[0]);

            Mock<ITransaction> transaction = new Mock<ITransaction>();
            transaction.Setup(t => t.GetHash()).Returns(hash.Object);

            return transaction.Object;
        }

        private List<IHash> GetHashList(int hashCount)
        {
            List<IHash> hashList = new List<IHash>();

            for (int i = 0; i < hashCount; i++)
                hashList.Add(GetHashFromByte((byte)i));

            return hashList;
        }

        private IHash GetHashFromByte(byte b)
        {
            Mock<IHash> hashMock = new Mock<IHash>();
            hashMock.Setup(h => h.GetHashBytes()).Returns(new byte[] { (byte)b });
            hashMock.Setup(t => t.Equals(It.IsAny<IHash>())).Returns<IHash>(t => t?.GetHashBytes()[0] == t.GetHashBytes()[0]);

            return hashMock.Object;
        }

        #endregion
        
        [Fact]
        public void Test()
        {
            List<IHash> hashList = GetHashList(HASH_COUNT);
            
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
            
            // create vertices with info A-Z
            byte i = (byte)'A';
            
            while (i <= 'Z')
            {
                int randomIndex = new Random().Next(HASH_COUNT);
                var nextHash = hashList.ElementAt(randomIndex);

                if (!pending.Keys.Contains(nextHash))
                {
                    pending[nextHash] = new List<ITransaction>();
                }

                var transaction = GetTransactionFromByte(i++);
                pending[nextHash].Add(transaction);
            }

            Assert.Equal(pending.Count, HASH_COUNT);
            
            TransactionExecutingManager transactionExecutingManager = new TransactionExecutingManager();
            transactionExecutingManager.TransactionDictionary = pending;
            transactionExecutingManager.Scheduler();
        }
    } 
}
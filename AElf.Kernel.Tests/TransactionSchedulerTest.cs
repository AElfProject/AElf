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
        
        private ITransaction GetTransaction( byte i)
        {
            
            
            Mock <ITransaction> transaction=new Mock<ITransaction>();
            
            Mock<IHash<ITransaction>> hash=new Mock<IHash<ITransaction>>();
            hash.Setup(h => h.GetHashBytes()).Returns(new byte[]{i});
            hash.Setup(t => t.Equals(It.IsAny<IHash>()))
                .Returns<IHash>(t => t? .GetHashBytes()[0] ==t.GetHashBytes()[0] );
            transaction.Setup(t => t.GetHash()).Returns(hash.Object);

            return transaction.Object;
        }
        
        [Fact]
        public void Test()
        {
            
            
            List<IHash> hashList=new List<IHash>();
            int hashCount = 5;
            int j = 0;
            while (hashCount>j++)
            {
                Mock<IHash> hash=new Mock<IHash>();
                hash.Setup(h => h.GetHashBytes()).Returns(new byte[]{(byte) j});
                hash.Setup(t => t.Equals(It.IsAny<IHash>()))
                    .Returns<IHash>(t => t? .GetHashBytes()[0] ==t.GetHashBytes()[0] );
               
                hashList.Add(hash.Object);
                
            }
            
            Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
            
            // create vertexs with info A-Z
            byte i = (byte)'A';
            
            while (i<='Z')
            {
                
                var transaction = GetTransaction(i++);
                Random random=new Random();
                var h=hashList.ElementAt(random.Next(hashCount));

                if (!pending.Keys.Contains(h))
                {
                    pending[h]=new List<ITransaction>();
                }
                pending[h].Add(transaction);
            }

            Assert.Equal(pending.Count, hashCount);
            
            TransactionExecutingManager transactionExecutingManager=new TransactionExecutingManager();
            transactionExecutingManager.TransactionDictionary=pending;
            transactionExecutingManager.Scheduler();
           

        }
        
    }

    
    
}
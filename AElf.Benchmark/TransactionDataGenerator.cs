using System;
using System.Collections.Generic;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types.CSharp;
using Akka.Util.Internal.Collections;
using Google.Protobuf;
using Org.BouncyCastle.Security;

namespace AElf.Benchmark
{
    public class TransactionDataGenerator
    {
        private int _totalNumber;
        public List<Hash> KeyDict;

        public TransactionDataGenerator(int maxNumber)
        {
            _totalNumber = maxNumber;
            KeyDict = new List<Hash>();
            for (int i = 0; i < maxNumber + 200; i++)
            {
                KeyDict.Add(Hash.Generate().ToAccount());
            }
        }

        private IEnumerable<KeyValuePair<Hash, Hash>> GenerateTransferAddressPair(int txCount, double conflictRate, ref Iterator<Hash> keyDictIter)
        {
            if (txCount > _totalNumber) throw new InvalidParameterException();
            var txAccountList = new List<KeyValuePair<Hash, Hash>>();
            
            int conflictTxCount = (int) (conflictRate * txCount);
            var conflictKeyPair = keyDictIter.Next();
            var conflictAddr = new Hash(conflictKeyPair);

            
            for (int i = 0; i < conflictTxCount; i++)
            {
                var senderKp = keyDictIter.Next();
                txAccountList.Add(new KeyValuePair<Hash, Hash>(senderKp, conflictAddr));
            }

            for (int i = 0; i < txCount - conflictTxCount; i++)
            {
                var senderKp = keyDictIter.Next();
                var receiverKp = keyDictIter.Next();
                txAccountList.Add(new KeyValuePair<Hash, Hash>(senderKp, receiverKp));
            }

            return txAccountList;
        }
        
        public List<ITransaction> GetTxsWithOneConflictGroup(Hash contractAddr, int txNumber, double conflictRate)
        {
            var keyDictIter = KeyDict.Iterator();
            
            var addrPairs = GenerateTransferAddressPair(txNumber, conflictRate, ref keyDictIter);
            var txList = GenerateTransferTransactions(contractAddr, addrPairs);

            return txList;
        }

        public List<ITransaction> GetMultipleGroupTx(int txNumber, int groupCount, Hash contractAddr)
        {
            if(txNumber > _totalNumber)  throw new InvalidParameterException();
            int txNumPerGroup = txNumber / groupCount;
            var keyDictIter = KeyDict.Iterator();
            List<ITransaction> txList = new List<ITransaction>();
            for (int i = 0; i < groupCount; i++)
            {
                var addrPair = GenerateTransferAddressPair(txNumPerGroup, 1, ref keyDictIter);
                var groupTxList = GenerateTransferTransactions(contractAddr, addrPair);
                txList.AddRange(groupTxList);
            }

            return txList;
        }
        
        public List<ITransaction> GenerateTransferTransactions(Hash tokenContractAddr, IEnumerable<KeyValuePair<Hash, Hash>> transferAddressPairs)
        {
            var resList = new List<ITransaction>();
            foreach (var addressPair in transferAddressPairs)
            {

                //var keyPair = KeyDict[addressPair.Key];
                ulong qty = 50;
                Transaction tx = new Transaction()
                {
                    From = addressPair.Key,
                    To = tokenContractAddr,
                    IncrementId = 0,
                    MethodName = "Transfer",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(addressPair.Key, addressPair.Value, (ulong)20)),
                };

                Hash txHash = tx.GetHash();
                resList.Add(tx);
            }

            return resList;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Types.CSharp;
using Akka.Util.Internal.Collections;
using Google.Protobuf;
using Org.BouncyCastle.Security;
using ServiceStack;

namespace AElf.Benchmark
{
    public class TransactionDataGenerator
    {
        public List<Hash> KeyList;
        private int _maxTxNumber;
        private int _maxGroupNumber;

        public TransactionDataGenerator(BenchmarkOptions opts)
        {
            if (opts.BenchmarkMethod == BenchmarkMethod.EvenGroup)
            {
                if (ReadFromFile(opts.AccountFileDir) == false)
                {
                    GenerateHashes(opts);
                }
            }
            else if (opts.BenchmarkMethod == BenchmarkMethod.GenerateAccounts)
            {
                GenerateHashes(opts);
            }
        }

        private void GenerateHashes(BenchmarkOptions opts)
        {
            if (opts.GroupRange.Count() != 2 && opts.GroupRange.ElementAt(0) <= opts.GroupRange.ElementAt(1))
            {
                Console.WriteLine("Group range option required, and should be [a, b] where a <= b");
                return;
            }
            _maxTxNumber = opts.TxNumber;
            _maxGroupNumber = opts.GroupRange.ElementAt(1);
            Console.WriteLine($"Generate account for {_maxTxNumber} from scratch");
            KeyList = new List<Hash>();
            for (int i = 0; i < _maxTxNumber + _maxGroupNumber; i++)
            {
                KeyList.Add(Hash.Generate().ToAccount());
            }
        }

        private IEnumerable<KeyValuePair<Hash, Hash>> GenerateTransferAddressPair(int txCount, double conflictRate, ref Iterator<Hash> keyDictIter)
        {
            if (txCount > _maxTxNumber) throw new InvalidParameterException();
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
            var keyDictIter = KeyList.Iterator();
            
            var addrPairs = GenerateTransferAddressPair(txNumber, conflictRate, ref keyDictIter);
            var txList = GenerateTransferTransactions(contractAddr, addrPairs);

            return txList;
        }

        public List<ITransaction> GetMultipleGroupTx(int txNumber, int groupCount, Hash contractAddr)
        {
            if(txNumber > _maxTxNumber)  throw new InvalidParameterException();
            int txNumPerGroup = txNumber / groupCount;
            var keyDictIter = KeyList.Iterator();
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
                Transaction tx = new Transaction()
                {
                    From = addressPair.Key,
                    To = tokenContractAddr,
                    IncrementId = 0,
                    MethodName = "Transfer",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(addressPair.Key, addressPair.Value, (ulong)20)),
                };
                
                resList.Add(tx);
            }

            return resList;
        }

        public void PersistAddrsToFile(string path)
        {
            var fullDirPath = System.IO.Path.GetFullPath(path);
            if (!Directory.Exists(fullDirPath))
            {
                Console.WriteLine("Dir " + fullDirPath + " not exist");
            }

            var sw = new StreamWriter(System.IO.Path.Combine(fullDirPath, "Account.dat"), false);
            
            sw.WriteLine(_maxTxNumber);
            sw.WriteLine(_maxGroupNumber);

            foreach (var addr in KeyList)
            {
                sw.WriteLine(addr.Value.ToByteArray().ToHex());
            }
            
            sw.Flush();

            Console.WriteLine($"Data file with {_maxTxNumber + _maxGroupNumber} addr is written into file {System.IO.Path.Combine(fullDirPath, "Account.dat")}");
        }

        public bool ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            var filePath = System.IO.Path.Combine(path, "Account.dat");
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine(filePath + " not exist");
                    return false;
                }
            
                var sr = new StreamReader(filePath);
                _maxTxNumber = sr.ReadLine().ToInt();
                _maxGroupNumber = sr.ReadLine().ToInt();

                if (_maxGroupNumber <= 0 || _maxTxNumber <= 0 || _maxTxNumber < _maxGroupNumber)
                {
                    Console.WriteLine($"Data at path {filePath} corrupted, should start with 2 line int (maxTxNumber and maxGroupNumber)");
                    return false;
                }
            
                KeyList = new List<Hash>();
                string addrStr;
                while ((addrStr = sr.ReadLine()) != null)
                {
                    KeyList.Add(new Hash(ByteArrayHelpers.FromHexString(addrStr)));
                }

                if (KeyList.Count != _maxTxNumber + _maxGroupNumber)
                {
                    Console.WriteLine($"Data at path {filePath} corrupted, number of the address({KeyList.Count}) don't match the maxTxNumber({_maxTxNumber}) + maxGroupNumber({_maxGroupNumber}) = {_maxTxNumber + _maxGroupNumber}");
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Data file read failed");
                return false;
            }

            Console.WriteLine($"Read addrs from file {filePath} successfully, Arguments of the data generator are adjusted to [_maxTxNumber: {_maxTxNumber}, _maxGroupNumber: {_maxGroupNumber}] because the data file path {path} are given");
            return true;
        }
    }
}
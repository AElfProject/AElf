using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using SharpRepository.Repository.Configuration;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.DPoS
{
    public class Process : CSharpSmartContract
    {
        private const int MiningTime = 4;

        private readonly Map _blockProducer = new Map("BPs");
        
        private readonly Map _dPoSInfoProducer = new Map("DPoS");

        private UInt64Value RoundsCount => 
            UInt64Value.Parser.ParseFrom(_dPoSInfoProducer.GetValueAsync("RoundsCount".CalculateHash()).Result);

        #region Block Producers
        
        public async Task<object> GetBlockProducers()
        {
            // Should be setted before
            var blockProducer = BlockProducer.Parser.ParseFrom(await _blockProducer.GetValueAsync("List".CalculateHash()));

            if (blockProducer.Nodes.Count < 1)
            {
                throw new ConfigurationErrorsException("No block producer.");
            }
            
            Api.Return(blockProducer);

            return blockProducer;
        }
        
        public async Task<object> SetBlockProducers()
        {
            List<string> miningNodes;
            
            //TODO: Temp impl.
            using (var file = 
                File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.DPoS/MiningNodes.txt")))
            {
                miningNodes = file.ReadLines().ToList();
            }

            var nodes = new BlockProducer();
            foreach (var node in miningNodes)
            {
                nodes.Nodes.Add(node);
            }

            if (nodes.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find block producers in related config file.");
            }
 
            await _blockProducer.SetValueAsync("List".CalculateHash(), nodes.ToByteArray());

            return nodes;
        }
        
        public async Task<object> SetBlockProducers(BlockProducer blockProducer)
        {
            if (blockProducer.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find mining nodes in related config file.");
            }

            await _blockProducer.SetValueAsync("List".CalculateHash(), blockProducer.ToByteArray());

            return blockProducer;
        }
        
        #endregion
        
        #region Genesis block methods
        
        public async Task<object> RandomizeOrderForFirstTwoRounds()
        {
            var blockProducers = (BlockProducer) await GetBlockProducers();
            var dict = new Dictionary<string, int>();
            
            // First round
            foreach (var node in blockProducers.Nodes)
            {
                dict.Add(node, new Random(GetTimestamp().GetHashCode()).Next(0, 1000));
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();
            for (var i = 0; i < enumerable.Count; i++)
            {
                if (i == 0)
                {
                    await SetValue(new Hash(Encoding.UTF8.GetBytes(enumerable[i])),
                        "IsEBP", new BoolValue {Value = true});
                    Hash key = RoundsCount.CalculateHashWith("EBP");
                    await _dPoSInfoProducer.SetValueAsync(key, new StringValue {Value = enumerable[0]}.ToByteArray());
                }
                await SetValue(new Hash(Encoding.UTF8.GetBytes(enumerable[i])),
                    "TimeSlot", GetTimestamp(i * 4));
                await SetValue(new Hash(Encoding.UTF8.GetBytes(enumerable[i])),
                    "Order", new Int32Value {Value = i + 1});
                if (i == enumerable.Count - 1)
                {
                    await _dPoSInfoProducer.SetValueAsync(
                        RoundsCount.CalculateHashWith("TimeToProduceExtraBlock"),
                        GetTimestamp(i * 4 + 4).ToByteArray());
                }
            }
            
            // Second round
            foreach (var node in blockProducers.Nodes)
            {
                dict[node] = new Random(GetTimestamp().GetHashCode()).Next(0, 1000);
            }
            
            sortedMiningNodes =
                from obj in dict
                orderby obj.Value
                select obj.Key;
            
            enumerable = sortedMiningNodes.ToList();
            for (var i = 0; i < enumerable.Count; i++)
            {
                if (i == 0)
                {
                    await SetValue(new Hash(Encoding.UTF8.GetBytes(enumerable[i])),
                        "IsEBP", new BoolValue {Value = true});
                    Hash key = RoundsCount.CalculateHashWith("EBP");
                    await _dPoSInfoProducer.SetValueAsync(key, new StringValue {Value = enumerable[0]}.ToByteArray());
                }
                await SetValue(new Hash(Encoding.UTF8.GetBytes(enumerable[i])),
                    "TimeSlot", GetTimestamp(i * 4));
                await SetValue(new Hash(Encoding.UTF8.GetBytes(enumerable[i])),
                    "Order", new Int32Value {Value = i + 1});
                if (i == enumerable.Count - 1)
                {
                    await _dPoSInfoProducer.SetValueAsync(
                        RoundsCount.CalculateHashWith("TimeToProduceExtraBlock"),
                        GetTimestamp(i * 4 + 4).ToByteArray());
                }
            }
            

            return null;
        }

        public async Task<object> RandomizeSignaturesForFirstRound()
        {
            var blockProducer = ((BlockProducer) await GetBlockProducers()).Nodes.ToList();
            var blockProducerCount = blockProducer.Count;

            for (var i = 0; i < blockProducerCount; i++)
            {
                await SetValue(new Hash(Encoding.UTF8.GetBytes(blockProducer[i])),
                    "Signature", GetTimestamp(i * 4 + 4));
            }

            return null;
        }
        
        #endregion

        public async Task<object> GenerateNextRoundOrder()
        {
            // Check the tx is generated by the extra-block-producer before
            var from = Api.GetTransaction().From;
            var accountAddress = from.Value.Take(18).ToArray();
            var dataProvider = GetDataProviderForCurrentRound(accountAddress);
            // ReSharper disable once InconsistentNaming
            var isEBP = BoolValue.Parser.ParseFrom(
                await dataProvider.GetAsync(
                    new Hash(RoundsCount.CalculateHashWith("IsEBP")))).Value;
            if (!isEBP)
            {
                return null;
            }
            
            var blockProducer = (BlockProducer) await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;
            
            var signatureDict = new Dictionary<Hash, string>();
            foreach (var node in blockProducer.Nodes)
            {
                var key = RoundsCount.CalculateHashWith("Signatures");
                signatureDict[Hash.Parser.ParseFrom(
                    await GetDataProviderForCurrentRound(
                        Encoding.UTF8.GetBytes(node).Take(18).ToArray()).GetAsync(key))] = node;
            }
            
            var orderDict = new Dictionary<int, string>();
            foreach (var sig in signatureDict.Keys)
            {
                var sigNum = BitConverter.ToUInt64(
                    BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
                var order = (int) sigNum % blockProducerCount;
                orderDict.Add(
                    orderDict.ContainsKey(order)
                        ? Enumerable.Range(0, blockProducerCount - 1).First(n => !orderDict.ContainsKey(n))
                        : order,
                    signatureDict[sig]);
            }
            
            for (var i = 0; i < orderDict.Count; i++)
            {
                var accountHash = new Hash(Encoding.UTF8.GetBytes(orderDict[i]));
                if (i == 0)
                {
                    await _dPoSInfoProducer.SetValueAsync(
                        RoundsCount.CalculateHashWith("FirstPlace"), accountHash.ToByteArray());
                }

                await SetValue(accountHash, "TimeSlot", GetTimestamp(i * 4));
                
                if (i == orderDict.Count - 1)
                {
                    await _dPoSInfoProducer.SetValueAsync(
                        RoundsCount.CalculateHashWith("TimeToProduceExtraBlock"),
                        GetTimestamp(i * 4 + 4).ToByteArray());
                }
            }

            return null;
        }

        public async Task<object> GetExtraBlockProducer()
        {
            Hash key = RoundsCount.CalculateHashWith("EBP");
            return StringValue.Parser.ParseFrom(await _dPoSInfoProducer.GetValueAsync(key));
        }

        public async Task<object> SetNextExtraBlockProducer()
        {
            var firstPlace = Hash.Parser.ParseFrom(await _dPoSInfoProducer.GetValueAsync(
                RoundsCount.CalculateHashWith("FirstPlace")));
            var firstPlaceDataProvider = GetDataProviderForCurrentRound(firstPlace.Value.Take(18).ToArray());
            var sig = Hash.Parser.ParseFrom(await firstPlaceDataProvider.GetAsync(
                RoundsCount.CalculateHashWith("Signature")));
            var sigNum = BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
            var blockProducer = (BlockProducer) await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;
            var order = (int) sigNum % blockProducerCount;
            // ReSharper disable once InconsistentNaming
            var nextEBP = blockProducer.Nodes[order];
            await _dPoSInfoProducer.SetValueAsync(RoundsCountAddOne(RoundsCount).CalculateHashWith("EBP"),
                new StringValue {Value = nextEBP}.ToByteArray());

            return null;
        }

        public async Task<object> GetTimeSlot(byte[] accountAddress = null)
        {
            var key = RoundsCount.CalculateHashWith("TimeSlot");
            var timeSlot = Timestamp.Parser.ParseFrom(
                await GetDataProviderForCurrentRound(accountAddress).GetAsync(key));

            Api.Return(timeSlot);
            
            return timeSlot;
        }

        public async Task<object> SetRoundsCount(ulong count)
        {
            await _dPoSInfoProducer.SetValueAsync("RoundsCount".CalculateHash(), 
                new UInt64Value {Value = count}.ToByteArray());

            return null;
        }

        public async Task<object> PublishOutValue(Hash outValue)
        {
            return await SetValueToFromAccount("Out", outValue);
        }

        public async Task<object> PublishInValue(Hash inValue)
        {
            return await SetValueToFromAccount("In", inValue);
        }
        
        public async Task<object> PublishSignature(Hash signature)
        {
            return await SetValueToFromAccount("Signature", signature);
        }

        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            if (member != null) await (Task<object>) member.Invoke(this, parameters);
        }

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestamp(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.Now.AddMinutes(offset));
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() > ts2.ToDateTime();
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForRoundRelatedData(ulong roundsCount, Hash blockProducerAccountHash)
        {
            return new Hash(new UInt64Value {Value = roundsCount}.CalculateHash())
                .CalculateHashWith(blockProducerAccountHash);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForRoundRelatedData(IMessage roundsCount, Hash blockProducerAccountHash)
        {
            return new Hash(roundsCount.CalculateHash()).CalculateHashWith(blockProducerAccountHash);
        }
        
        private IDataProvider GetDataProviderForSpecificRound(UInt64Value roundsCount, byte[] blockProducerAddress)
        {
            return _blockProducer.GetSubDataProvider(BitConverter.ToString(blockProducerAddress) + roundsCount.Value);
        }
        
        private IDataProvider GetDataProviderForCurrentRound(byte[] blockProducerAddress)
        {
            return _blockProducer.GetSubDataProvider(BitConverter.ToString(blockProducerAddress) + RoundsCount.Value);
        }
        
        private async Task<object> SetValueToFromAccount(string valueType, IMessage value)
        {
            var accountHash = Api.GetTransaction().From;
            var accountAddress = accountHash.Value.Take(18).ToArray();
            Hash key = RoundsCount.CalculateHashWith(valueType);
            await GetDataProviderForCurrentRound(accountAddress).SetAsync(key, value.ToByteArray());

            return null;
        }
        
        private async Task<object> SetValue(Hash accountHash, string valueType, IMessage value)
        {
            var accountAddress = accountHash.Value.Take(18).ToArray();
            Hash key = RoundsCount.CalculateHashWith(valueType);
            await GetDataProviderForCurrentRound(accountAddress).SetAsync(key, value.ToByteArray());

            return null;
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountAddOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current++;
            return new UInt64Value {Value = current};
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountMinusOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current--;
            return new UInt64Value {Value = current};
        }
    }
}
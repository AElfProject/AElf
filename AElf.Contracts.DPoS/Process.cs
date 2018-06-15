using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        #region Maps

        public readonly Map BlockProducer = new Map("BlockProducer");
        
        public readonly Map ExtraBlockProducer = new Map("ExtraBlockProducer");
        
        public readonly Map TimeSlots = new Map("TimeSlots");
        
        public readonly Map Signatures = new Map("Signatures");
        
        public readonly Map RoundsCount = new Map("RoundsCount");
        
        public readonly Map Ins = new Map("Ins");
        
        public readonly Map Outs = new Map("Outs");

        #endregion

        #region Mining nodes
        
        public async Task<object> GetBlockProducer()
        {
            // Should be setted before
            var blockProducer = Kernel.BlockProducer.Parser.ParseFrom(await BlockProducer.GetValue(Hash.Zero));

            if (blockProducer.Nodes.Count < 1)
            {
                throw new ConfigurationErrorsException("No block producer.");
            }
            
            Api.Return(blockProducer);

            return blockProducer;
        }
        
        public async Task<object> SetBlockProducer()
        {
            List<string> miningNodes;
                
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
 
            await BlockProducer.SetValueAsync(Hash.Zero, nodes.ToByteArray());

            return nodes;
        }
        
        public async Task<object> SetBlockProducer(BlockProducer blockProducer)
        {
            if (blockProducer.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find mining nodes in related config file.");
            }

            await BlockProducer.SetValueAsync(Hash.Zero, blockProducer.ToByteArray());

            return null;
        }
        
        #endregion
        
        public async Task<object> RandomizeOrderForFirstTwoRounds()
        {
            var miningNodes = (BlockProducer) await GetBlockProducer();
            var dict = new Dictionary<string, int>();
            
            // First round
            foreach (var node in miningNodes.Nodes)
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
                var key = CalculateKeyForRoundRelatedData(1, enumerable[i]);
                await TimeSlots.SetValueAsync(key, GetTimestamp(i * 4 + 4).ToByteArray());
            }
            
            // Second round
            foreach (var node in miningNodes.Nodes)
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
                var key = CalculateKeyForRoundRelatedData(2, enumerable[i]);
                await TimeSlots.SetValueAsync(key, GetTimestamp(i * 4 + miningNodes.Nodes.Count * 4 + 8).ToByteArray());
            }

            return null;
        }

        public async Task<object> RandomizeSignaturesForFirstRound()
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var blockProducer = ((BlockProducer) await GetBlockProducer()).Nodes.ToList();
            var blockProducerCount = blockProducer.Count;

            for (var i = 0; i < blockProducerCount; i++)
            {
                var key = CalculateKeyForRoundRelatedData(roundsCount.Value, blockProducer[i]);
                await Signatures.SetValueAsync(key, Hash.Generate().ToByteArray());
            }

            return null;
        }

        public async Task<object> GenerateNextRoundOrder()
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();

            // Check the tx is generated by the extra-block-producer before
            var from = Api.GetTransaction().From;
            var extraBlockProducer = Hash.Parser.ParseFrom(await ExtraBlockProducer.GetValue(
                roundsCount.CalculateHash()));
            if (from != extraBlockProducer)
            {
                return null;
            }
            
            var blockProducer = (BlockProducer) await GetBlockProducer();
            var blockProducerCount = blockProducer.Nodes.Count;
            
            var signatureDict = new Dictionary<Hash, string>();
            foreach (var node in blockProducer.Nodes)
            {
                var key = CalculateKeyForRoundRelatedData(roundsCount, node);
                signatureDict[Hash.Parser.ParseFrom(await Signatures.GetValue(key))] = node;
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
                var key = CalculateKeyForRoundRelatedData(roundsCount, orderDict[i]);
                await TimeSlots.SetValueAsync(key, GetTimestamp(i * 4 + 4).ToByteArray());
            }

            return null;
        }

        public async Task<object> GetExtraBlockProducer()
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            
            return await ExtraBlockProducer.GetValue(roundsCount.CalculateHash());
        }

        public async Task<object> SetExtraBlockProducer()
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            
            // TODO: need to get the signature of first place
            // await ExtraBlockProducer.SetValueAsync(roundsCount.CalculateHash(), )
            throw new NotImplementedException();
        }

        public async Task<object> GetTimeSlot(string accountAddress)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = CalculateKeyForRoundRelatedData(roundsCount, accountAddress);
            var timeSlot = await TimeSlots.GetValue(key);

            Api.Return(new BytesValue {Value = ByteString.CopyFrom(timeSlot)});
            
            return timeSlot;
        }

        public async Task<object> SetTimeSlot(string accountAddress, Timestamp timestamp)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = CalculateKeyForRoundRelatedData(roundsCount, accountAddress);

            await TimeSlots.SetValueAsync(key, timestamp.ToByteArray());

            return null;
        }

        public async Task<object> AbleToMine(string accountAddress)
        {
            var assignedTimeSlot = (Timestamp) await GetTimeSlot(accountAddress);
            var timeSlotEnd = assignedTimeSlot.ToDateTime().AddSeconds(MiningTime).ToTimestamp();

            var can = CompareTimestamp(assignedTimeSlot, GetTimestamp()) && CompareTimestamp(timeSlotEnd, assignedTimeSlot);
            
            Api.Return(new BoolValue {Value = can});
            
            return can;
        }
        
        public async Task<object> CalculateSignature(Hash accountHash)
        {
            // Get signatures of last round.
            var currentRoundCount = (UInt64Value) await GetRoundsCount();
            var lastRoundCount = new UInt64Value {Value = currentRoundCount.Value - 1};

            var add = Hash.Zero;
            var blockProducer = (BlockProducer) await GetBlockProducer();
            foreach (var node in blockProducer.Nodes)
            {
                var key = CalculateKeyForRoundRelatedData(lastRoundCount.Value, node);
                Hash lastSignature = await Signatures.GetValue(key);
                add = add.CalculateHashWith(lastSignature);
            }

            var inValue = (Hash) await Ins.GetValue(accountHash);
            Hash signature = inValue.CalculateHashWith(add);
            
            Api.Return(signature);

            return signature;
        }

        public async Task<object> SetRoundsCount(ulong count)
        {
            await RoundsCount.SetValueAsync(Hash.Zero, new UInt64Value {Value = count}.ToByteArray());

            return null;
        }

        public async Task<object> GetRoundsCount()
        {
            var count = UInt64Value.Parser.ParseFrom(await RoundsCount.GetValue(Hash.Zero));
            
            Api.Return(count);
            
            return count;
        }

        public async Task<object> PublishOutValue(Hash outValue)
        {
            var accountHash = Api.GetTransaction().From;
            
            var roundsCount = (UInt64Value) await GetRoundsCount();
            Hash key = accountHash.CalculateHashWith(roundsCount);
            await Outs.SetValueAsync(key, outValue.ToByteArray());

            return null;
        }

        public async Task<object> PublishInValue(string accountAddress, Hash inValue)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = CalculateKeyForRoundRelatedData(roundsCount, accountAddress);
            var timeSlot = Timestamp.Parser.ParseFrom(await TimeSlots.GetValue(key));

            if (!CompareTimestamp(GetTimestamp(-MiningTime), timeSlot)) 
                return false;

            await Ins.SetValueAsync(key, inValue.ToByteArray());

            return true;
        }
        
        public async Task<object> PublishSignature(string accountAddress, Hash signature)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = CalculateKeyForRoundRelatedData(roundsCount, accountAddress);
            var timeSlot = Timestamp.Parser.ParseFrom(await TimeSlots.GetValue(key));

            if (!CompareTimestamp(GetTimestamp(-MiningTime), timeSlot)) 
                return false;

            await Signatures.SetValueAsync(key, signature.ToByteArray());

            return true;
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
        private Hash CalculateKeyForRoundRelatedData(ulong roundsCount, string blockProducer)
        {
            return new Hash(new UInt64Value {Value = roundsCount}.CalculateHash()).CalculateHashWith(blockProducer);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForRoundRelatedData(IMessage roundsCount, string blockProducer)
        {
            return new Hash(roundsCount.CalculateHash()).CalculateHashWith(blockProducer);
        }
    }
}
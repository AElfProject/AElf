using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public Map MiningNodes = new Map("MiningNodes");
        
        public Map TimeSlots = new Map("TimeSlots");
        
        public Map Signatures = new Map("Signatures");
        
        public Map RoundsCount = new Map("RoundsCount");
        
        public Map Ins = new Map("Ins");
        
        public Map Outs = new Map("Outs");
        
        public async Task<object> SetMiningNodes()
        {
            List<string> miningNodes;
                
            using (var file = 
                File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.DPoS/MiningNodes.txt")))
            {
                miningNodes = file.ReadLines().ToList();
            }

            var nodes = new MiningNodes();
            foreach (var node in miningNodes)
            {
                nodes.Nodes.Add(new Hash(ByteString.CopyFromUtf8(node)));
            }

            if (nodes.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find mining nodes in related config file.");
            }

            await MiningNodes.SetValueAsync(Hash.Zero, nodes.ToByteArray());

            return nodes;
        }
        
        public async Task<object> GetMiningNodes()
        {
            // Should be set before
            var miningNodes = Kernel.MiningNodes.Parser.ParseFrom(
                await MiningNodes.GetValue(Hash.Zero));

            if (miningNodes.Nodes.Count < 1)
            {
                throw new ConfigurationErrorsException("No mining nodes.");
            }
            
            Api.Return(new MiningNodes {Nodes =
            {
                Hash.Generate(), Hash.Generate()
            }});

            return miningNodes;
        }

        public async Task<object> SetMiningNodes(MiningNodes miningNodes)
        {
            if (miningNodes.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find mining nodes in related config file.");
            }

            await MiningNodes.SetValueAsync(Hash.Zero, miningNodes.ToByteArray());

            return null;
        }

        public async Task<object> RandomizeOrderForFirstTwoRounds()
        {
            var miningNodes = (MiningNodes) await GetMiningNodes(Hash.Zero);
            var dict = new Dictionary<Hash, int>();
            
            // First round
            foreach (var node in miningNodes.Nodes)
            {
                dict.Add(node, new Random(GetTime().LastOrDefault()).Next(0, 1000));
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();
            for (var i = 0; i < enumerable.Count; i++)
            {
                Hash key = enumerable[i].CalculateHashWith(new Hash(new UInt64Value {Value = 1}.CalculateHash()));
                await TimeSlots.SetValueAsync(key, GetTime(i * 4 + 4));
            }
            
            // Second round
            foreach (var node in miningNodes.Nodes)
            {
                dict[node] = new Random(GetTime().LastOrDefault()).Next(0, 1000);
            }
            
            sortedMiningNodes =
                from obj in dict
                orderby obj.Value
                select obj.Key;
            
            enumerable = sortedMiningNodes.ToList();
            for (var i = 0; i < enumerable.Count; i++)
            {
                Hash key = enumerable[i].CalculateHashWith(new Hash(new UInt64Value {Value = 1}.CalculateHash()));
                await TimeSlots.SetValueAsync(key, GetTime(i * 4 + miningNodes.Nodes.Count * 4 + 8));
            }

            return null;
        }

        public async Task<object> RandomizeSignaturesForFirstRound()
        {
            Hash roundsCountHash = ((UInt64Value) await GetRoundsCount()).CalculateHash();
            var miningNodes = ((MiningNodes) await GetMiningNodes(Hash.Zero)).Nodes.ToList();
            var miningNodesCount = miningNodes.Count;

            for (var i = 0; i < miningNodesCount; i++)
            {
                Hash key = miningNodes[i].CalculateHashWith(roundsCountHash);
                await Signatures.SetValueAsync(key, Hash.Generate().ToByteArray());
            }

            return null;
        }

        public async Task<object> GenerateNextRoundOrder()
        {
            
        }

        public async Task<object> GetTimeSlot(Hash accountHash)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = accountHash.CalculateHashWith((Hash) roundsCount.CalculateHash());
            var timeSlot = await TimeSlots.GetValue(key);

            Api.Return(new BytesValue {Value = ByteString.CopyFrom(timeSlot)});
            
            return timeSlot;
        }

        public async Task<object> AbleToMine(Hash accountHash)
        {
            var assignedTimeSlot = (byte[]) await GetTimeSlot(accountHash);
            var timeSlotEnd = DateTime
                .Parse(Encoding.UTF8.GetString(assignedTimeSlot))
                .AddMinutes(MiningTime)
                .ToString("yyyy-MM-dd HH:mm:ss.ffffff")
                .ToUtf8Bytes();

            var can = CompareBytes(assignedTimeSlot, GetTime()) && CompareBytes(timeSlotEnd, assignedTimeSlot);
            
            Api.Return(new BoolValue {Value = can});
            
            return can;
        }
        
        public async Task<object> CalculateSignature(Hash accountHash)
        {
            // Get signatures of last round.
            var currentRoundCount = (UInt64Value) await GetRoundsCount();
            var lastRoundCount = new UInt64Value {Value = currentRoundCount.Value - 1};
            Hash roundCountHash = lastRoundCount.CalculateHash();

            var add = Hash.Zero;
            var miningNodes = (MiningNodes) await GetMiningNodes(Hash.Zero);
            foreach (var node in miningNodes.Nodes)
            {
                Hash key = node.CalculateHashWith(roundCountHash);
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

        public async Task<object> GenerateExtraBlockTransactions()
        {
            throw new NotImplementedException();
        }

        public async Task<object> PublishOutValue(Hash outValue)
        {
            var accountHash = Api.GetTransaction().From;
            
            var roundsCount = (UInt64Value) await GetRoundsCount();
            Hash key = accountHash.CalculateHashWith(roundsCount);
            await Outs.SetValueAsync(key, outValue.ToByteArray());

            return null;
        }

        public async Task<object> PublishInValue(Hash accountHash, Hash inValue)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = accountHash.CalculateHashWith((Hash) roundsCount.CalculateHash());
            var timeSlot = await TimeSlots.GetValue(key);

            if (!CompareBytes(GetTime(-MiningTime), timeSlot)) 
                return false;

            await Ins.SetValueAsync(key, inValue.ToByteArray());

            return true;
        }
        
        public async Task<object> PublishSignature(Hash accountHash, Hash signature)
        {
            var roundsCount = (UInt64Value) await GetRoundsCount();
            var key = accountHash.CalculateHashWith((Hash) roundsCount.CalculateHash());
            var timeSlot = await TimeSlots.GetValue(key);

            if (!CompareBytes(GetTime(-MiningTime), timeSlot)) 
                return false;

            await Signatures.SetValueAsync(key, signature.ToByteArray());

            return true;
        }

        public async Task<object> PreVerification(Hash inValue, Hash outValue)
        {
            var valid = inValue.CalculateHash() == outValue;

            Api.Return(new BoolValue {Value = valid});

            return valid;
        }
        
        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            await (Task<object>)member.Invoke(this, parameters);
        }

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        private byte[] GetTime(int offset = 0)
        {
            return DateTime.Now.AddMinutes(offset).ToString("yyyy-MM-dd HH:mm:ss.ffffff").ToUtf8Bytes();
        }

        private bool CompareBytes(byte[] bytes1, byte[] bytes2)
        {
            //Caonnot compare
            if (bytes1.Length != bytes2.Length)
            {
                return false;
            }

            var length = bytes1.Length;
            for (var i = 0; i < length; i++)
            {
                if (bytes1[i] > bytes2[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
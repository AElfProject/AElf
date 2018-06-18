using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoS
    {
        //TODO: Better read this value in config file, at least not hard coded.
        private const int MiningTime = 4;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Global
        public IDataProvider DPoSDataProvider;

        private IDataProvider _blockProducerDataProvider;
        private IDataProvider _dPoSDataProvider;

        private readonly MainChainNode _node;

        // ReSharper disable once MemberCanBePrivate.Global
        public UInt64Value RoundsCount => 
            UInt64Value.Parser.ParseFrom(_dPoSDataProvider.GetAsync("RoundsCount".CalculateHash()).Result);

        private bool _isChainIdSetted;

        private readonly IWorldStateManager _worldStateManager;

        public DPoS(IWorldStateManager worldStateManager, MainChainNode node)
        {
            _worldStateManager = worldStateManager;
            _node = node;
        }

        public async Task<DPoS> OfChain(Hash chainId)
        {
            await _worldStateManager.OfChain(chainId);
            
            DPoSDataProvider = new AccountDataProvider(
                chainId, Path.CalculatePointerForAccountZero(chainId), _worldStateManager).
                GetDataProvider();

            _blockProducerDataProvider = DPoSDataProvider.GetDataProvider("BPs");
            _dPoSDataProvider = DPoSDataProvider.GetDataProvider("DPoS");
            
            _isChainIdSetted = true;
            
            return this;
        }
        
        // For genesis block and block producers
        #region Get Txs to sync state

        public List<ITransaction> GetTxsForGenesisBlock()
        {
            return new List<ITransaction>
            {
                new Transaction
                {
                    From = Hash.Zero,
                    To = Hash.Zero,
                    IncrementId = 0,
                    Fee = 3, //TODO: TBD
                    MethodName = "RandomizeOrderForFirstTwoRounds"
                },
                new Transaction
                {
                    From = Hash.Zero,
                    To = Hash.Zero,
                    IncrementId = 1, //TODO: not sure
                    Fee = 3,
                    MethodName = "RandomizeSignaturesForFirstRound"
                }
            };
        }

        public List<ITransaction> GetTxsForExtraBlock()
        {
            var newCount = RoundsCountAddOne(RoundsCount);
            return new List<ITransaction>
            {
                new Transaction
                {
                    From = _node.PublicKey,
                    To = Hash.Zero,
                    IncrementId = 0,
                    Fee = 3, //TODO: TBD
                    MethodName = "GenerateNextRoundOrder"
                },
                new Transaction
                {
                    From = _node.PublicKey,
                    To = Hash.Zero,
                    IncrementId = 1, //TODO: not sure
                    Fee = 3, //TODO: TBD
                    MethodName = "SetNextExtraBlockProducer"
                },
                new Transaction
                {
                    From = _node.PublicKey,
                    To = Hash.Zero,
                    IncrementId = 2, //TODO: not sure
                    Fee = 3, //TODO: TBD
                    MethodName = "SetRoundsCount",
                    Params = ByteString.CopyFrom(new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                UlongVal = newCount.Value
                            }
                        }
                    }.ToByteArray())
                }
            };
        }

        public List<ITransaction> GetTxsForNormalBlock(Hash outValue, Hash sigValue)
        {
            return new List<ITransaction>
            {
                new Transaction
                {
                    From = _node.PublicKey,
                    To = Hash.Zero,
                    IncrementId = 0,
                    Fee = 3,
                    MethodName = "PublishOutValue",
                    Params = ByteString.CopyFrom(new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                HashVal = outValue
                            }
                        }
                    }.ToByteArray())
                },
                new Transaction
                {
                    From = _node.PublicKey,
                    To = Hash.Zero,
                    IncrementId = 1,
                    Fee = 3,
                    MethodName = "PublishSignature",
                    Params = ByteString.CopyFrom(new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                HashVal = sigValue
                            }
                        }
                    }.ToByteArray())
                }
            };
        }

        public bool TryToGetTxForPublishInValue(Hash inValue, out ITransaction tx)
        {
            if (!TimeToGenerateExtraBlock().Result)
            {
                tx = null;
                return false;
            }
            tx =  new Transaction
            {
                From = _node.PublicKey,
                To = Hash.Zero,
                IncrementId = 0,
                Fee = 3,
                MethodName = "PublishInValue",
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            HashVal = inValue
                        }
                    }
                }.ToByteArray())
            };
            return true;
        }
        
        #endregion

        #region Pre-verification

        public bool PreVerification(Hash inValue, Hash outValue)
        {
            return inValue.CalculateHash() == outValue;
        }

        #endregion

        #region Mining nodes

        public async Task<BlockProducer> GetBlockProducer()
        {
            return BlockProducer.Parser.ParseFrom(await _blockProducerDataProvider.GetAsync(Hash.Zero));
        }

        #endregion

        #region Time slots

        public async Task<Timestamp> GetTimeSlotOf(byte[] accountAddress)
        {
            var dataProvider = GetDataProviderForCurrentRound(accountAddress);
            Hash key = RoundsCount.CalculateHashWith("TimeSlot");
            return Timestamp.Parser.ParseFrom(await dataProvider.GetAsync(key));
        }
        
        #endregion

        #region Ins, Outs, Signatures

        public async Task<Hash> GetInValueOf(byte[] accountAddress)
        {
            var dataProvider = GetDataProviderForCurrentRound(accountAddress);
            Hash key = RoundsCount.CalculateHashWith("In");
            return Hash.Parser.ParseFrom(await dataProvider.GetAsync(key));
        }
        
        public async Task<Hash> GetOutValueOf(byte[] accountAddress)
        {
            var dataProvider = GetDataProviderForCurrentRound(accountAddress);
            Hash key = RoundsCount.CalculateHashWith("Out");
            return Hash.Parser.ParseFrom(await dataProvider.GetAsync(key));
        }
        
        public async Task<Hash> GetSignatureOf(byte[] accountAddress)
        {
            var dataProvider = GetDataProviderForCurrentRound(accountAddress);
            Hash key = RoundsCount.CalculateHashWith("Signature");
            return Hash.Parser.ParseFrom(await dataProvider.GetAsync(key));
        }
        
        public async Task<Hash> CalculateSignature(Hash inValue)
        {
            var add = Hash.Zero;
            var blockProducer = await GetBlockProducer();
            foreach (var node in blockProducer.Nodes)
            {
                Hash key = RoundsCount.CalculateHashWith("Signature");
                var lastSignature = Hash.Parser.ParseFrom(
                    await GetDataProviderForSpecificRound(RoundsCountMinusOne(RoundsCount), 
                        Encoding.UTF8.GetBytes(node).Take(18).ToArray()).GetAsync(key));
                add = add.CalculateHashWith(lastSignature);
            }
            
            return inValue.CalculateHashWith(add);
        }
        
        #endregion
        
        public async Task<bool> AbleToMine(byte[] accountAddress)
        {
            var assignedTimeSlot = await GetTimeSlotOf(accountAddress);
            var timeSlotEnd = assignedTimeSlot.ToDateTime().AddSeconds(MiningTime).ToTimestamp();

            return CompareTimestamp(assignedTimeSlot, GetTimestamp()) 
                   && CompareTimestamp(timeSlotEnd, assignedTimeSlot);
        }

        public async Task<bool> TimeToGenerateExtraBlock()
        {
            var time = Timestamp.Parser.ParseFrom(
                await _dPoSDataProvider.GetAsync(RoundsCount.CalculateHashWith("TimeToProduceExtraBlock")));
            return CompareTimestamp(time, GetTimestamp());
        }
        
        private void Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using DPoS.");
            }
        }

        private async Task<bool> BlockProducerIdentityVerification(IEnumerable<byte> address)
        {
            var blockProducer = await GetBlockProducer();
            // todo : double-check
            return blockProducer.Nodes.Contains(address.ToString());
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private async Task<bool> ExtraBlockProducerIdentityVerification(byte[] address)
        {
            Hash key = RoundsCount.CalculateHashWith("IsEBP");
            return BoolValue.Parser.ParseFrom(await GetDataProviderForCurrentRound(address).GetAsync(key)).Value;
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

        private IDataProvider GetDataProviderForSpecificRound(UInt64Value roundsCount, byte[] blockProducerAddress = null)
        {
            Interlocked.CompareExchange(ref blockProducerAddress, null, _node.Address);
            return _blockProducerDataProvider.GetDataProvider(BitConverter.ToString(blockProducerAddress) + roundsCount.Value);
        }
        
        private IDataProvider GetDataProviderForCurrentRound(byte[] blockProducerAddress = null)
        {
            Interlocked.CompareExchange(ref blockProducerAddress, null, _node.Address);
            return _blockProducerDataProvider.GetDataProvider(BitConverter.ToString(blockProducerAddress) + RoundsCount.Value);
        }
    }
}
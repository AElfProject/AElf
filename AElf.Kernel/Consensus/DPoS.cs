using System;
using System.Collections.Generic;
using System.IO;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Extensions;
using AElf.Kernel.Node;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoS
    {
        private readonly Hash _publicKey;
        
        public byte[] ContractCode
        {
            get
            {
                byte[] code;
                using (var file = 
                    File.OpenRead(System.IO.Path.GetFullPath(
                        "../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/AElf.Contracts.DPoS.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }

        public DPoS(Hash publicKey)
        {
            _publicKey = publicKey;
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
                    MethodName = "RandomizeInfoForFirstTwoRounds"
                }
            };
        }

        public List<ITransaction> GetTxsForExtraBlock()
        {
            return new List<ITransaction>
            {
                new Transaction
                {
                    From = _publicKey,
                    To = Hash.Zero,
                    IncrementId = 0,
                    Fee = 3, //TODO: TBD
                    MethodName = "GenerateNextRoundOrder"
                },
                new Transaction
                {
                    From = _publicKey,
                    To = Hash.Zero,
                    IncrementId = 1, //TODO: not sure
                    Fee = 3, //TODO: TBD
                    MethodName = "SetNextExtraBlockProducer"
                },
                new Transaction
                {
                    From = _publicKey,
                    To = Hash.Zero,
                    IncrementId = 2, //TODO: not sure
                    Fee = 3, //TODO: TBD
                    MethodName = "SetRoundsCount"
                }
            };
        }

        public List<ITransaction> GetTxsForNormalBlock(Hash outValue, Hash sigValue)
        {
            return new List<ITransaction>
            {
                new Transaction
                {
                    From = _publicKey,
                    To = Hash.Zero,
                    IncrementId = 0,
                    Fee = 3,
                    MethodName = "PublishOutValueAndSignature",
                    Params = ByteString.CopyFrom(new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                HashVal = outValue
                            },
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
            tx =  new Transaction
            {
                From = _publicKey,
                To = Hash.Zero,
                IncrementId = 0,
                Fee = 3,
                MethodName = "TryToPublishInValue",
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
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly ECKeyPair _keyPair;

        public Hash AccountHash => _keyPair.GetAddress();

        public DPoS(ECKeyPair keyPair)
        {
            _keyPair = keyPair;
        }
        
        // For genesis block and block producers
        #region Get Txs to sync state

        public List<ITransaction> GetTxsForGenesisBlock(ulong incrementId, string blockProducerStr, Hash contractAccountHash)
        {
            var txs = new List<ITransaction>
            {
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId++,
                    MethodName = "SetBlockProducers",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom(new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                StrVal = blockProducerStr
                            }
                        }
                    }.ToByteArray())
                },
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId++,
                    MethodName = "GenerateInfoForFirstTwoRounds",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom()
                }
            };

            return txs.Select(t =>
            {
                var signer = new ECSigner();
                var signature = signer.Sign(_keyPair, t.GetHash().GetHashBytes());

                // Update the signature
                ((Transaction) t).R = ByteString.CopyFrom(signature.R);
                ((Transaction) t).S = ByteString.CopyFrom(signature.S);
                return t;
            }).ToList();
        }

        public Transaction GetAbleToMineTx(ulong incrementId, Hash contractAccountHash)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "AbleToMine",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom()
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        // ReSharper disable once InconsistentNaming
        public Transaction GetIsBPTx(ulong incrementId, Hash contractAccountHash)
        {
            var accoutAddress = AddressHashToString(AccountHash.ToAccount());
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "IsBP",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            StrVal = accoutAddress
                        }
                    }
                }.ToByteArray())
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        // ReSharper disable once InconsistentNaming
        public Transaction GetIsTimeToProduceExtraBlockTx(ulong incrementId, Hash contractAccountHash)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "IsTimeToProduceExtraBlock",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom()
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        // ReSharper disable once InconsistentNaming
        public Transaction GetAbleToProduceExtraBlockTx(ulong incrementId, Hash contractAccountHash)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "AbleToProduceExtraBlock",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom()
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        public List<ITransaction> GetTxsForExtraBlock(ulong incrementId, Hash contractAccountHash)
        {
            var txs = new List<ITransaction>
            {
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId++,
                    MethodName = "GenerateNextRoundOrder",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom()
                },
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId++,
                    MethodName = "SetNextExtraBlockProducer",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom()
                },
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId,
                    MethodName = "SetRoundsCount",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom()
                }
            };

            return txs.Select(t =>
            {
                var signer = new ECSigner();
                var signature = signer.Sign(_keyPair, t.GetHash().GetHashBytes());

                // Update the signature
                ((Transaction) t).R = ByteString.CopyFrom(signature.R);
                ((Transaction) t).S = ByteString.CopyFrom(signature.S);
                return t;
            }).ToList();
        }

        public List<ITransaction> GetTxsForNormalBlock(ulong incrementId, Hash contractAccountHash, ulong roundsCount,
            Hash outValue, Hash sig)
        {
            var foo = AddressHashToString(outValue);
            var txs = new List<ITransaction>
            {
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId,
                    MethodName = "PublishOutValueAndSignatureDebug",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom(new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                StrVal = AddressHashToString(outValue)
                            },
                            new Param
                            {
                                StrVal = AddressHashToString(sig)
                            },
                            new Param
                            {
                                UlongVal = roundsCount
                            }
                        }
                    }.ToByteArray())
                }
            };

            return txs.Select(t =>
            {
                var signer = new ECSigner();
                var signature = signer.Sign(_keyPair, t.GetHash().GetHashBytes());

                // Update the signature
                ((Transaction) t).R = ByteString.CopyFrom(signature.R);
                ((Transaction) t).S = ByteString.CopyFrom(signature.S);
                return t;
            }).ToList();
        }

        public Transaction GetOutValueOfMeTx(ulong incrementId, Hash contractAccountHash, ulong roundsCount)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "GetOutValueOf",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            StrVal = AddressHashToString(_keyPair.GetAddress())
                        },
                        new Param
                        {
                            UlongVal = roundsCount
                        }
                    }
                }.ToByteArray())
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        public Transaction GetInValueOfMeTx(ulong incrementId, Hash contractAccountHash, ulong roundsCount)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "GetInValueOf",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            StrVal = AddressHashToString(_keyPair.GetAddress())
                        },
                        new Param
                        {
                            UlongVal = roundsCount
                        }
                    }
                }.ToByteArray())
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
        
        public Transaction GetSignatureValueOfMeTx(ulong incrementId, Hash contractAccountHash, ulong roundsCount)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "GetSignatureOf",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            StrVal = AddressHashToString(_keyPair.GetAddress())
                        },
                        new Param
                        {
                            UlongVal = roundsCount
                        }
                    }
                }.ToByteArray())
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        public Transaction TryToGetTxForPublishInValue(ulong incrementId, Hash contractAccountHash)
        {
            var tx =  new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "TryToPublishInValue",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(new Parameters
                {
                    Params =
                    {
                        new Param
                        {
                            HashVal = Hash.Generate()
                        }
                    }
                }.ToByteArray())
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);
            
            return tx;
        }

        // ReSharper disable once InconsistentNaming
        public Transaction GetDPoSInfoToStringTx(ulong incrementId, Hash contractAccountHash)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "GetDPoSInfoToString",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom()
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        public Transaction GetCalculateSignatureTx(ulong incrementId, Hash contractAccountHash, Hash inValue)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "CalculateSignature",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
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
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        public Transaction GetRoundsCountTx(ulong incrementId, Hash contractAccountHash)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "GetRoundsCount",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom()
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
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
        
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().Value.ToBase64();
        }

        private Hash AddressStringToHash(string accountAddress)
        {
            return Convert.FromBase64String(accountAddress);
        }
    }
}
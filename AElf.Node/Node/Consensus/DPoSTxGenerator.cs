﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Node;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using AElf.Kernel;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoSTxGenerator
    {
        private readonly ECKeyPair _keyPair;

        public Hash AccountHash => _keyPair.GetAddress();

        public DPoSTxGenerator(ECKeyPair keyPair)
        {
            _keyPair = keyPair;
        }
        
        // For first extra block and block producers
        #region Get Txs to sync state

        public Transaction GetTxToSyncFirstExtraBlock(ulong incrementId, Hash contractAccountHash,
            DPoSInfo dPoSInfo, BlockProducer blockProducer)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "InitializeAElfDPoS",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(blockProducer.ToByteArray(), dPoSInfo.ToByteArray()))
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        public Transaction GetTxToSyncExtraBlock(ulong incrementId, Hash contractAccountHash,
            // ReSharper disable once InconsistentNaming
            RoundInfo currentRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "UpdateAElfDPoS",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(
                    currentRoundInfo.ToByteArray(),
                    nextRoundInfo.ToByteArray(), 
                    nextEBP.ToByteArray()))
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(_keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        public IEnumerable<ITransaction> GetTxsForNormalBlock(ulong incrementId, Hash contractAccountHash, ulong roundNumber,
            Hash outValue, Hash sig)
        {
            var txs = new List<ITransaction>
            {
                new Transaction
                {
                    From = AccountHash,
                    To = contractAccountHash,
                    IncrementId = incrementId,
                    MethodName = "PublishOutValueAndSignature",
                    P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                    Params = ByteString.CopyFrom(
                        ParamsPacker.Pack(
                            new UInt64Value {Value = roundNumber}.ToByteArray(),
                            new StringValue {Value = _keyPair.GetAddress().ToHex().RemoveHexPrefix()}.ToByteArray(),
                            outValue.ToByteArray(), 
                            sig.ToByteArray()))
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
        
        public Transaction GetTxToPublishInValueTx(ulong incrementId, Hash contractAccountHash,
            Hash inValue, UInt64Value roundNumber)
        {
            var tx = new Transaction
            {
                From = AccountHash,
                To = contractAccountHash,
                IncrementId = incrementId,
                MethodName = "PublishInValue",
                P = ByteString.CopyFrom(_keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(
                    roundNumber.ToByteArray(),
                    new StringValue {Value = _keyPair.GetAddress().ToHex().RemoveHexPrefix()}.ToByteArray(),
                    inValue.ToByteArray()))
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
    }
}
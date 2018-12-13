using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.Consensus.Tests
{
    /// <summary>
    /// In these test cases, we just care about the sequences, not the time slots.
    /// </summary>
    [UseAutofacTestFramework]
    public class ConsensusProcessTest
    {
        private readonly ConsensusContractShim _consensusContract;
        private readonly MockSetup _mock;

        private readonly List<ECKeyPair> _miners = new List<ECKeyPair>();

        private int MiningInterval => 1;

        public ConsensusProcessTest(MockSetup mock)
        {
            _mock = mock;
            _consensusContract = new ConsensusContractShim(mock);

            for (var i = 0; i < 17; i++)
            {
                _miners.Add(new KeyPairGenerator().Generate());
            }
        }

        [Fact]
        public void InitialTermTest()
        {
            InitialTerm(_miners[0]);
            Assert.Equal(string.Empty, _consensusContract.TransactionContext.Trace.StdErr);

            // Check the information of first round.
            var firstRound = _consensusContract.GetRoundInfo(_miners[0], 1);
            Assert.True(firstRound.RoundNumber == 1);
            Assert.True(firstRound.RealTimeMinersInfo.Count == _miners.Count);
            Assert.True(firstRound.MiningInterval == MiningInterval);
            // Only one Extra Block Producer.
            Assert.True(firstRound.RealTimeMinersInfo.Values.Count(m => m.IsExtraBlockProducer) == 1);
            // Signature is not null.
            Assert.True(firstRound.RealTimeMinersInfo.Values.Count(m => m.Signature == null) == 0);
            // In Value and Out Value is null.
            Assert.True(firstRound.RealTimeMinersInfo.Values.Count(m => m.InValue == null) == _miners.Count);
            Assert.True(firstRound.RealTimeMinersInfo.Values.Count(m => m.OutValue == null) == _miners.Count);

            // Check the information of second round.
            var secondRound = _consensusContract.GetRoundInfo(_miners[0], 2);
            Assert.True(secondRound.RoundNumber == 2);
            Assert.True(secondRound.RealTimeMinersInfo.Count == _miners.Count);
            Assert.True(secondRound.MiningInterval == MiningInterval);
            // Only one Extra Block Producer.
            Assert.True(secondRound.RealTimeMinersInfo.Values.Count(m => m.IsExtraBlockProducer) == 1);
            // Signature is null.
            Assert.True(secondRound.RealTimeMinersInfo.Values.Count(m => m.Signature == null) == _miners.Count);
            // In Value and Out Value is null.
            Assert.True(secondRound.RealTimeMinersInfo.Values.Count(m => m.InValue == null) == _miners.Count);
            Assert.True(secondRound.RealTimeMinersInfo.Values.Count(m => m.OutValue == null) == _miners.Count);
            
            // Check produced block count.
            Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].ProducedBlocks);

            // Check the information of not generated round.
            try
            {
                _consensusContract.GetRoundInfo(_miners[0], 3);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.RoundNumberNotFound, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        [Fact]
        public void PackageOutValueTest()
        {
            InitialTerm(_miners[0]);
            var firstRound = _consensusContract.GetRoundInfo(_miners[0], 1);

            Assert.Equal((ulong) 1, firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].ProducedBlocks);

            var outValue = Hash.Generate();
            var signatureOfInitialization = firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].Signature;
            var signature = Hash.Generate();// Should be update to round info, we'll see.
            var toPackage = new ToPackage
            {
                OutValue = outValue,
                RoundId = firstRound.RoundId,
                Signature = signature
            };
            _consensusContract.PackageOutValue(_miners[0], toPackage);
            Assert.Equal(string.Empty, _consensusContract.TransactionContext.Trace.StdErr);

            // Check the round information.
            firstRound = _consensusContract.GetRoundInfo(_miners[0], 1);
            // Signature not changed.
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].Signature ==
                        signatureOfInitialization);
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].OutValue == outValue);
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].InValue == null);
            Assert.Equal((ulong) 2, firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].ProducedBlocks);
        }
        
        [Fact]
        public void PackageOutValueTest_RoundIdNotMatched()
        {
            InitialTerm(_miners[0]);
            var firstRound = _consensusContract.GetRoundInfo(_miners[0], 1);

            var toPackage = new ToPackage
            {
                OutValue = Hash.Generate(),
                RoundId = firstRound.RoundId + 1,// Wrong round id.
                Signature = Hash.Generate()
            };

            try
            {
                _consensusContract.PackageOutValue(_miners[0], toPackage);
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.RoundIdNotMatched, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        [Fact]
        public void BroadcastInValueTest()
        {
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            // Before
            var firstRound = InitialTermAndPackageOutValue(_miners[0], outValue);
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].OutValue == outValue);
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].InValue == null);

            _consensusContract.BroadcastInValue(_miners[0], new ToBroadcast
            {
                InValue = inValue,
                RoundId = firstRound.RoundId
            });
            Assert.Equal(string.Empty, _consensusContract.TransactionContext.Trace.StdErr);

            // After
            firstRound = _consensusContract.GetRoundInfo(_miners[0], 1);
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].OutValue == outValue);
            Assert.True(firstRound.RealTimeMinersInfo[_miners[0].PublicKey.ToHex()].InValue == inValue);
        }
        
        [Fact]
        public void BroadcastInValueTest_OutValueIsNull()
        {
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            InitialTerm(_miners[0]);
            
            var firstRound= _consensusContract.GetRoundInfo(_miners[0], 1);
            try
            {
                _consensusContract.BroadcastInValue(_miners[0], new ToBroadcast
                {
                    InValue = outValue,
                    RoundId = firstRound.RoundId
                });
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.OutValueIsNull, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }
        
        [Fact]
        public void BroadcastInValueTest_InValueNotMatchToOutValue()
        {
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var notMatchOutValue = Hash.FromMessage(outValue);
            
            var firstRound = InitialTermAndPackageOutValue(_miners[0], notMatchOutValue);

            try
            {
                _consensusContract.BroadcastInValue(_miners[0], new ToBroadcast
                {
                    InValue = inValue,
                    RoundId = firstRound.RoundId
                });
            }
            catch (Exception)
            {
                Assert.Equal(GlobalConfig.InValueNotMatchToOutValue, _consensusContract.TransactionContext.Trace.StdErr);
            }
        }

        private void InitialTerm(ECKeyPair starterKeyPair)
        {
            var initialTerm =
                new Miners {PublicKeys = {_miners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(MiningInterval);
            _consensusContract.InitialTerm(starterKeyPair, initialTerm);
        }

        private Round InitialTermAndPackageOutValue(ECKeyPair starterKeyPair, Hash outValue)
        {
            InitialTerm(starterKeyPair);
            var firstRound = _consensusContract.GetRoundInfo(starterKeyPair, 1);
            _consensusContract.PackageOutValue(starterKeyPair, new ToPackage
            {
                OutValue = outValue,
                RoundId = firstRound.RoundId,
                Signature = Hash.Default
            });
            
            return _consensusContract.GetRoundInfo(starterKeyPair, 1);
        }

        private Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(_mock.ChainId.DumpByteArray(), keyPair.PublicKey);
        }
    }
}
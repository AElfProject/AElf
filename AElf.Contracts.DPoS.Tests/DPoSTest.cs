using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.DPoS.Tests
{
    [UseAutofacTestFramework]
    // ReSharper disable once InconsistentNaming
    public class DPoSTest
    {
        private readonly BlockProducer _blockProducer;
        
        private readonly TestDPoSContractShim _contractShim;
        
        public DPoSTest(TestDPoSContractShim contractShim)
        {
            _contractShim = contractShim;

            _blockProducer = new BlockProducer();
            _blockProducer.Nodes.AddRange(Enumerable.Range(0, 17)
                .Select(i => Convert.ToBase64String(Hash.Generate().ToAccount().Value.ToArray())));
        }
        
        [Fact]
        public void BlockProducderTest()
        {
            var txResultOfSet = _contractShim.SetBlockProducers(_blockProducer);
            
            Assert.Equal(_blockProducer, txResultOfSet);

            var txResultOfGet = _contractShim.GetBlockProducers();
            
            Assert.Equal(_blockProducer, txResultOfGet);
        }
        
        [Fact]
        public void RandomizeInfoForFirstTwoRoundsTest()
        {
            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);

            var txResult = _contractShim.RandomizeInfoForFirstTwoRounds();
            
            Assert.True(txResult.RoundInfo.Count == 2);
            
            Assert.Equal(17, txResult.RoundInfo[1].Info.Count);
            
            //Only one EPB in each round
            Assert.Equal(1, txResult.RoundInfo[0].Info.Count(i => i.Value.IsEBP));
            Assert.Equal(1, txResult.RoundInfo[1].Info.Count(i => i.Value.IsEBP));
        }

        // ReSharper disable once InconsistentNaming
        [Fact]
        public void GetEBPOfCurrentRoundTest()
        {
            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();
            
            //Actually get EBP of second round
            var txResult = _contractShim.GetEBPOfCurrentRound();

            Assert.Contains(txResult, _blockProducer.Nodes);
        }

        [Fact]
        public void CalculateSignaturesTest()
        {
            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();
            var inValues = new List<Hash>();
            inValues.AddRange(Enumerable.Range(0, 16).Select(i => Hash.Generate()));

            var signatures = new List<Hash>();
            foreach (var value in inValues)
            {
                signatures.Add(_contractShim.CalculateSignature(value));
            }
            
            Assert.NotEmpty(signatures);
        }

        [Fact]
        public void PublishOutValueAndSignatureTest()
        {
            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();
            var inValues = new List<Hash>();
            inValues.AddRange(Enumerable.Range(0, 17).Select(i => Hash.Generate()));
            var outValues = inValues.Select(v => new Hash(v.CalculateHash())).ToList();
            var signatures = new List<Hash>();
            foreach (var value in inValues)
            {
                signatures.Add(_contractShim.CalculateSignature(value));
            }

            var txResult = _contractShim.PublishOutValueAndSignature(_blockProducer, outValues, signatures);
            
            Assert.NotNull(txResult);
        }
        
        [Fact]
        public void GenerateNextRoundOrderTest()
        {
            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();
            var inValues = new List<Hash>();
            inValues.AddRange(Enumerable.Range(0, 17).Select(i => Hash.Generate()));
            var outValues = inValues.Select(v => new Hash(v.CalculateHash())).ToList();
            var signatures = new List<Hash>();
            foreach (var value in inValues)
            {
                signatures.Add(_contractShim.CalculateSignature(value));
            }
            _contractShim.PublishOutValueAndSignature(_blockProducer, outValues, signatures);

            var extraBlockProducer = _contractShim.GetEBPOfCurrentRound();

            var txResult = _contractShim.GenerateNextRoundOrder(extraBlockProducer);
            
            Assert.NotNull(txResult);
        }
    }
}
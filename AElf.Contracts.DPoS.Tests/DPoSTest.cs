using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
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

            Assert.Equal(17, txResult.Info.Select(i => i.Value.OutValue).Count());
            Assert.Equal(17, txResult.Info.Select(i => i.Value.Signature).Count());
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
            
            Assert.Equal(17, txResult.Info.Count);
        }

        [Fact]
        public void SetNextExtraBlockProducerTest()
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
            _contractShim.GenerateNextRoundOrder(_contractShim.GetEBPOfCurrentRound());
            
            var txResult = _contractShim.SetNextExtraBlockProducer();

            Assert.Contains(txResult, _blockProducer.Nodes);
        }

        [Fact]
        public void AbleToMineTest_Never()
        {
            const long timeout = 5;

            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();

            // ReSharper disable once InconsistentNaming
            var notBP = AddressHashToString(Hash.Generate().ToAccount());
            var intervalSequence = GetIntervalObservable(timeout);
            var ableToMine = false;
            long actualTime = 0;
            intervalSequence.Subscribe
            (
                x =>
                {
                    if (_contractShim.AbleToMine(notBP))
                    {
                        ableToMine = true;
                    }

                    actualTime = x;
                }
            );
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            
            Assert.True(actualTime > 17);
            Assert.Equal(false, ableToMine);
        }
        
        [Fact]
        public void AbleToMineTest_True()
        {
            const long timeout = 5;

            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();

            // ReSharper disable once InconsistentNaming
            var aBP = _blockProducer.Nodes[7]; //Pick one block producer
            var intervalSequence = GetIntervalObservable(timeout);
            var ableToMine = false;
            long actualTime = 0;
            intervalSequence.Subscribe
            (
                x =>
                {
                    if (_contractShim.AbleToMine(aBP))
                    {
                        ableToMine = true;
                    }

                    actualTime = x;
                }
            );
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            
            Assert.True(actualTime > 17);
            Assert.Equal(true, ableToMine);
        }
        
        [Fact]
        public void TimeToProduceExtraBlockTest()
        {
            const long timeout = 5;

            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();

            // ReSharper disable once InconsistentNaming
            var intervalSequence = GetIntervalObservable(timeout);
            var canProduceExtraBlock = false;
            long actualTime = 0;
            long firstAbleTime = 0;
            intervalSequence.Subscribe
            (
                x =>
                {
                    if (_contractShim.IsTimeToProduceExtraBlock())
                    {
                        canProduceExtraBlock = true;
                        if (firstAbleTime == 0)
                        {
                            firstAbleTime = x;
                        }
                    }

                    actualTime = x;
                }
            );
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            
            Assert.True(actualTime > 34);
            Assert.True(firstAbleTime > 10);
            Assert.True(firstAbleTime < 34);
            Assert.Equal(true, canProduceExtraBlock);
        }

        [Fact]
        public void AbleToProduceExtraBlockTest_Never()
        {
            const long timeout = 5;

            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();

            // ReSharper disable once InconsistentNaming
            var notEvernBP = AddressHashToString(Hash.Generate().ToAccount());
            var intervalSequence = GetIntervalObservable(timeout);
            var ableToMine = false;
            long actualTime = 0;
            intervalSequence.Subscribe
            (
                x =>
                {
                    if (_contractShim.AbleToProduceExtraBlock(notEvernBP))
                    {
                        ableToMine = true;
                    }

                    actualTime = x;
                }
            );
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            
            Assert.True(actualTime > 17);
            Assert.Equal(false, ableToMine);
        }
        
        [Fact]
        public void AbleToProduceExtraBlockTest_True()
        {
            const long timeout = 5;

            //Preparation
            _contractShim.SetBlockProducers(_blockProducer);
            _contractShim.RandomizeInfoForFirstTwoRounds();

            // ReSharper disable once InconsistentNaming
            var eBP = _contractShim.GetEBPOfCurrentRound();
            var intervalSequence = GetIntervalObservable(timeout);
            var ableToMine = false;
            long actualTime = 0;
            long firstAbleTime = 0;
            intervalSequence.Subscribe
            (
                x =>
                {
                    if (_contractShim.AbleToProduceExtraBlock(eBP))
                    {
                        ableToMine = true;
                        if (firstAbleTime == 0)
                        {
                            firstAbleTime = x;
                        }
                    }

                    actualTime = x;
                }
            );
            Thread.Sleep(TimeSpan.FromSeconds(timeout));
            
            Assert.True(actualTime > 17);
            Assert.True(firstAbleTime > 10);
            Assert.True(firstAbleTime < 34);
            Assert.Equal(true, ableToMine);
        }
        
        private static IObservable<long> GetIntervalObservable(long timeout)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(100)).Timeout(TimeSpan.FromSeconds(timeout));
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
using System;
using System.Collections.Generic;
using Xunit;
using AElf.WebApp.Application.Chain;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit.Abstractions;
using Xunit.Sdk;


namespace AElf.WebApp.Application.Chain.Tests.infrastructure
{
    public class MiningSequenceRepositoryTest: WebAppTestBase
    {
        private readonly IMiningSequenceRepository _miningSequenceRepository;
         

         public MiningSequenceRepositoryTest(ITestOutputHelper outputHelper):base(outputHelper)
         {
             _miningSequenceRepository = GetRequiredService<IMiningSequenceRepository>();

         }

         [Fact]
        public void AddMiningSequence_Test()
        {
          MiningSequenceDto MiningSequenceA = new MiningSequenceDto
        {
            Pubkey ="", Behaviour = "", BlockHeight = 1, MiningTime = new Timestamp(),
            PreviousBlockHash = ""
        };
             //arrange
             var miningSequence = _miningSequenceRepository;
             var miningList = new List<MiningSequenceDto>();
             //act
             miningList.Add(MiningSequenceA);
             var expected = miningList;
             miningSequence.AddMiningSequence(MiningSequenceA);
            var result = miningSequence.GetAllMiningSequences();
 
         //assert
          result.ShouldBe(expected);
        }

        [Fact]
        public void DeleteMiningSequence_Test()
        {
            MiningSequenceDto MiningSequenceA = new MiningSequenceDto
            {
                Pubkey ="", Behaviour = "", BlockHeight = 1, MiningTime = new Timestamp(),
                PreviousBlockHash = ""
            };
            MiningSequenceDto MiningSequenceB = new MiningSequenceDto
            {
                Pubkey = MiningSequenceA.Pubkey, Behaviour = MiningSequenceA.Behaviour,
                BlockHeight = MiningSequenceA.BlockHeight, MiningTime = MiningSequenceA.MiningTime,
                PreviousBlockHash = MiningSequenceA.PreviousBlockHash
            };
            var miningSequenceRepositoryB = _miningSequenceRepository;
            miningSequenceRepositoryB.AddMiningSequence(MiningSequenceB);
            miningSequenceRepositoryB.ClearMiningSequences(0);
            var result = miningSequenceRepositoryB.GetAllMiningSequences();
            result.ShouldBeEmpty();



        }
        
        
    }
}

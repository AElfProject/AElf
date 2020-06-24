using System;
using System.Collections.Generic;
using Xunit;
using AElf.WebApp.Application.Chain;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit.Sdk;


namespace AElf.WebApp.Application.Chain.Tests.infrastructure
{
    public class MiningSequenceRepository_Test
    {
        
         private readonly MiningSequenceDto MiningSequenceA = new MiningSequenceDto
        {
            Pubkey ="", Behaviour = "", BlockHeight = 1, MiningTime = new Timestamp(),
            PreviousBlockHash = ""
        };
         
        [Fact]
        public void Test_AddMiningSequence()
        {
             //arrange
             var miningSequence=new MiningSequenceRepository();
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
        public void Test_DeleteMiningSequence()
        {
            MiningSequenceDto MiningSequenceB = new MiningSequenceDto
            {
                Pubkey = MiningSequenceA.Pubkey, Behaviour = MiningSequenceA.Behaviour,
                BlockHeight = MiningSequenceA.BlockHeight, MiningTime = MiningSequenceA.MiningTime,
                PreviousBlockHash = MiningSequenceA.PreviousBlockHash
            };
            var miningSequenceRepositoryB = new MiningSequenceRepository();
            miningSequenceRepositoryB.AddMiningSequence(MiningSequenceB);
            miningSequenceRepositoryB.ClearMiningSequences(0);
            var result = miningSequenceRepositoryB.GetAllMiningSequences();
            result.ShouldBeEmpty();



        }
        
        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.DPoS.SideChain
{
    public class DPoSSideChainTester
    {
        public const int MiningInterval = 4000;
        public int MinersCount { get; set; } = 3;

        public int ChainId { get; set; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        public List<ECKeyPair> MinersKeyPairs { get; set; } = new List<ECKeyPair>();

        public List<ContractTester<DPoSSideChainTestAElfModule>> Testers { get; set; } =
            new List<ContractTester<DPoSSideChainTestAElfModule>>();

        public ContractTester<DPoSSideChainTestAElfModule> SingleTester { get; set; }

        public Address DPoSSideChainContractAddress { get; set; }

        public void InitialTesters()
        {
            for (var i = 0; i < MinersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                MinersKeyPairs.Add(keyPair);
                var tester = new ContractTester<DPoSSideChainTestAElfModule>(ChainId, keyPair);

                AsyncHelper.RunSync(
                    () => tester.InitialSideChainAsync());
                Testers.Add(tester);
            }

            DPoSSideChainContractAddress = Testers[0].GetConsensusContractAddress();
        }

        public void InitialSingleTester()
        {
            SingleTester = new ContractTester<DPoSSideChainTestAElfModule>(ChainId, CryptoHelpers.GenerateKeyPair());
            AsyncHelper.RunSync(
                () => SingleTester.InitialSideChainAsync());
            DPoSSideChainContractAddress = SingleTester.GetConsensusContractAddress();
        }

        public DPoSTriggerInformation GetTriggerInformationForInitialTerm(IReadOnlyList<ECKeyPair> stubMiners)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = stubMiners[0].PublicKey.ToHex(),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Miners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000
            };
        }

        public DPoSTriggerInformation GetTriggerInformationForNormalBlock(string publicKey, Hash currentInValue,
            Hash previousInValue = null)
        {
            if (previousInValue == null) previousInValue = Hash.Empty;

            return new DPoSTriggerInformation
            {
                PublicKey = publicKey,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                PreviousInValue = previousInValue,
                CurrentInValue = currentInValue
            };
        }

        public DPoSTriggerInformation GetTriggerInformationForNextRoundOrTerm(string publicKey, Timestamp timestamp,
            bool isBootMiner = true)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = publicKey,
                Timestamp = timestamp,
                IsBootMiner = isBootMiner
            };
        }
    }
}
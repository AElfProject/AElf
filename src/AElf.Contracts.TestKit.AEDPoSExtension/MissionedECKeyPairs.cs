using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class MissionedECKeyPairs
    {
        public static readonly IEnumerable<ECKeyPair> InitialKeyPairs =
            SampleECKeyPairs.KeyPairs
                .Take(AEDPoSExtensionConstants.InitialKeyPairCount);

        public static readonly IEnumerable<ECKeyPair> CoreDataCenterKeyPairs =
            SampleECKeyPairs.KeyPairs
                .Skip(AEDPoSExtensionConstants.InitialKeyPairCount)
                .Take(AEDPoSExtensionConstants.CoreDataCenterKeyPairCount);

        public static readonly IEnumerable<ECKeyPair> ValidationDataCenterKeyPairs =
            SampleECKeyPairs.KeyPairs
                .Skip(AEDPoSExtensionConstants.InitialKeyPairCount +
                      AEDPoSExtensionConstants.CoreDataCenterKeyPairCount)
                .Take(AEDPoSExtensionConstants.ValidationDataCenterKeyPairCount);
        
        public static readonly IEnumerable<ECKeyPair> CitizenKeyPairs =
            SampleECKeyPairs.KeyPairs
                .Skip(AEDPoSExtensionConstants.InitialKeyPairCount +
                      AEDPoSExtensionConstants.CoreDataCenterKeyPairCount + 
                      AEDPoSExtensionConstants.ValidationDataCenterKeyPairCount)
                .Take(AEDPoSExtensionConstants.CitizenKeyPairsCount);
    }
}
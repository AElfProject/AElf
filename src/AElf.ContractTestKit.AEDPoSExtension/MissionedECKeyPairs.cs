using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AElf.Cryptography.ECDSA;

namespace AElf.ContractTestKit.AEDPoSExtension
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class MissionedECKeyPairs
    {
        public static readonly IEnumerable<ECKeyPair> InitialKeyPairs =
            SampleAccount.Accounts
                .Take(AEDPoSExtensionConstants.InitialKeyPairCount).Select(a => a.KeyPair);

        public static readonly IEnumerable<ECKeyPair> CoreDataCenterKeyPairs =
            SampleAccount.Accounts
                .Skip(AEDPoSExtensionConstants.InitialKeyPairCount)
                .Take(AEDPoSExtensionConstants.CoreDataCenterKeyPairCount).Select(a => a.KeyPair);

        public static readonly IEnumerable<ECKeyPair> ValidationDataCenterKeyPairs =
            SampleAccount.Accounts
                .Skip(AEDPoSExtensionConstants.InitialKeyPairCount +
                      AEDPoSExtensionConstants.CoreDataCenterKeyPairCount)
                .Take(AEDPoSExtensionConstants.ValidationDataCenterKeyPairCount).Select(a => a.KeyPair);
        
        public static readonly IEnumerable<ECKeyPair> CitizenKeyPairs =
            SampleAccount.Accounts
                .Skip(AEDPoSExtensionConstants.InitialKeyPairCount +
                      AEDPoSExtensionConstants.CoreDataCenterKeyPairCount + 
                      AEDPoSExtensionConstants.ValidationDataCenterKeyPairCount)
                .Take(AEDPoSExtensionConstants.CitizenKeyPairsCount).Select(a => a.KeyPair);
    }
}
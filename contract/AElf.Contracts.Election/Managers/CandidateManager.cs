using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Election.Managers
{
    public class CandidateManager : ICandidateManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<string, CandidateInformation> _candidateInformationMap;
        private readonly SingletonState<PubkeyList> _candidateList;
        private readonly MappedState<string, bool> _bannedPubkeyMap;
        private readonly SingletonState<PubkeyList> _initialMinerList;
        private readonly MappedState<string, Address> _candidateAdminMap;
        private readonly MappedState<string, string> _candidateReplacementMap;

        public CandidateManager(CSharpSmartContractContext context,
            MappedState<string, CandidateInformation> candidateInformationMap,
            SingletonState<PubkeyList> candidateList,
            MappedState<string, bool> bannedPubkeyMap,
            SingletonState<PubkeyList> initialMinerList,
            MappedState<string, Address> candidateAdminMap,
            MappedState<string, string> candidateReplacementMap)
        {
            _context = context;
            _candidateInformationMap = candidateInformationMap;
            _candidateList = candidateList;
            _bannedPubkeyMap = bannedPubkeyMap;
            _initialMinerList = initialMinerList;
            _candidateAdminMap = candidateAdminMap;
            _candidateReplacementMap = candidateReplacementMap;
        }

        public void AddCandidate(byte[] candidatePubkey, Address address)
        {
            var pubkey = candidatePubkey.ToHex();
            var pubkeyByteString = ByteString.CopyFrom(candidatePubkey);

            if (_bannedPubkeyMap[pubkey])
            {
                throw new AssertionException($"Candidate {pubkey} already banned before.");
            }

            if (_initialMinerList.Value.Value.Contains(pubkeyByteString))
            {
                throw new AssertionException("Initial miner cannot announce election.");
            }

            if (!_candidateList.Value.Value.Contains(pubkeyByteString))
            {
                _candidateList.Value.Value.Add(pubkeyByteString);
            }

            var candidateInformation = _candidateInformationMap[pubkey];
            if (candidateInformation != null)
            {
                if (candidateInformation.IsCurrentCandidate)
                {
                    throw new AssertionException($"{pubkey} already announced election.");
                }

                _candidateInformationMap[pubkey].IsCurrentCandidate = true;
                _candidateInformationMap[pubkey].AnnouncementTransactionId = _context.OriginTransactionId;
            }
            else
            {
                _candidateInformationMap[pubkey] = new CandidateInformation
                {
                    AnnouncementTransactionId = _context.OriginTransactionId,
                    Pubkey = pubkey,
                    IsCurrentCandidate = true
                };
            }

            _candidateAdminMap[pubkey] = address;
        }

        public CandidateInformation GetCandidateInformation(string pubkey)
        {
            AssertCandidateExists(pubkey);
            return _candidateInformationMap[pubkey];
        }

        public void AssertCandidateValid(string pubkey)
        {
            var candidateInformation = GetCandidateInformation(pubkey);
            if (!candidateInformation.IsCurrentCandidate)
            {
                throw new AssertionException($"Candidate {pubkey} maybe quited election.");
            }
        }

        private void AssertCandidateExists(string pubkey)
        {
            if (_candidateInformationMap[pubkey] == null)
            {
                throw new AssertionException($"Candidate {pubkey} not found.");
            }
        }
    }
}
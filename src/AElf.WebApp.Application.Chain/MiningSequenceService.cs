using System.Collections.Generic;
using System.Linq;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IMiningSequenceService : IApplicationService
    {
        List<MiningSequenceDto> GetMiningSequences(int count = 0);
        void AddMiningInformation(MiningSequenceDto miningInformationUpdated);
    }

    public class MiningSequenceService : IMiningSequenceService
    {
        private const int KeepRecordsCount = 256;
        private readonly IMiningSequenceRepository _miningSequenceRepository;

        public MiningSequenceService(IMiningSequenceRepository miningSequenceRepository)
        {
            _miningSequenceRepository = miningSequenceRepository;
        }

        public List<MiningSequenceDto> GetMiningSequences(int count = 0)
        {
            var sequences = _miningSequenceRepository.GetAllMiningSequences();
            sequences.Reverse();
            return sequences.Take(count < sequences.Count ? count : sequences.Count).ToList();
        }

        public void AddMiningInformation(MiningSequenceDto miningInformationUpdated)
        {
            _miningSequenceRepository.AddMiningSequence(miningInformationUpdated);
            _miningSequenceRepository.ClearMiningSequences(KeepRecordsCount);
        }
    }
}
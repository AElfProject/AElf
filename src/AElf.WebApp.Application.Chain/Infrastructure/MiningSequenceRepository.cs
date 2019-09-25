using System.Collections.Generic;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.Infrastructure
{
    public interface IMiningSequenceRepository
    {
        void AddMiningSequence(MiningSequenceDto miningSequenceDto);
        List<MiningSequenceDto> GetAllMiningSequences();
        void ClearMiningSequences(int keep);
    }

    public class MiningSequenceRepository : IMiningSequenceRepository, ISingletonDependency
    {
        private readonly List<MiningSequenceDto> _miningSequenceDtos = new List<MiningSequenceDto>();

        public void AddMiningSequence(MiningSequenceDto miningSequenceDto)
        {
            _miningSequenceDtos.AddIfNotContains(miningSequenceDto);
        }

        public List<MiningSequenceDto> GetAllMiningSequences()
        {
            return _miningSequenceDtos;
        }

        public void ClearMiningSequences(int keep)
        {
            var count = _miningSequenceDtos.Count - keep;
            _miningSequenceDtos.RemoveRange(0, count > 0 ? count : 0);
        }
    }
}
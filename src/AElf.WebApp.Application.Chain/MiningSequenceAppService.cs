using System.Collections.Generic;
using System.Linq;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IMiningSequenceAppService : IApplicationService
    {
        List<MiningSequenceDto> GetMiningSequences(int count = 0);
    }

    public class MiningSequenceAppService : IMiningSequenceAppService
    {
        private readonly IMiningSequenceRepository _miningSequenceRepository;

        public MiningSequenceAppService(IMiningSequenceRepository miningSequenceRepository)
        {
            _miningSequenceRepository = miningSequenceRepository;
        }

        public List<MiningSequenceDto> GetMiningSequences(int count = 0)
        {
            var sequences = _miningSequenceRepository.GetAllMiningSequences();
            sequences.Reverse();
            return sequences.Take(count < sequences.Count ? count : sequences.Count).ToList();
        }
    }
}
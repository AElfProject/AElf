using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.CodeCheck.Application
{
    internal class CodeCheckValidationProvider : IBlockValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IContractReaderFactory<ACS0Container.ACS0Stub> _contractReaderFactory;
        private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;

        public CodeCheckValidationProvider(ISmartContractAddressService smartContractAddressService,
            IContractReaderFactory<ACS0Container.ACS0Stub> contractReaderFactory,
            ICheckedCodeHashProvider checkedCodeHashProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _contractReaderFactory = contractReaderFactory;
            _checkedCodeHashProvider = checkedCodeHashProvider;
        }

        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            var genesisContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
            var deployedBloom = new ContractDeployed().ToLogEvent(genesisContractAddress).GetBloom();
            if (!deployedBloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray()))) return true;

            var blockHash = block.GetHash();
            var codeHashList = await _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = blockHash,
                BlockHeight = block.Header.Height,
                ContractAddress = genesisContractAddress
            }).GetContractCodeHashListByDeployBlockHeight.CallAsync(new Int64Value { Value = block.Header.Height });

            return codeHashList.Value.All(codeHash => _checkedCodeHashProvider.IsCodeHashExists(new BlockIndex
            {
                BlockHash = blockHash,
                BlockHeight = block.Header.Height
            }, codeHash));
        }
    }
}
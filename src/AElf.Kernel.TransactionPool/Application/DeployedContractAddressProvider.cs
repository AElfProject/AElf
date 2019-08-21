using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class DeployedContractAddressProvider : IDeployedContractAddressProvider
    {
        private AddressList _addressList = new AddressList();

        private readonly IBlockchainService _blockchainService;
        private readonly IZeroContractReaderFactory _zeroContractReaderFactory;

        public DeployedContractAddressProvider(IBlockchainService blockchainService,
            IZeroContractReaderFactory zeroContractReaderFactory)
        {
            _blockchainService = blockchainService;
            _zeroContractReaderFactory = zeroContractReaderFactory;
        }

        public async Task<AddressList> GetDeployedContractAddressListAsync()
        {
            if (_addressList.Value.Count != 0)
            {
                return _addressList;
            }

            var chain = await _blockchainService.GetChainAsync();
            _addressList = await _zeroContractReaderFactory.Create(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }).GetDeployedContractAddressList.CallAsync(new Empty());
            return _addressList;
        }

        public void AddDeployedContractAddress(Address address)
        {
            _addressList.Value.Add(address);
        }
    }
}
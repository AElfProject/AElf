using AElf.Contracts.Whitelist;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NFTMinter.Managers
{
    internal class WhitelistManager : IWhitelistManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<string, long, Address, Hash> _whitelistIdMap;
        private readonly WhitelistContractContainer.WhitelistContractReferenceState _whitelistContract;

        public WhitelistManager(CSharpSmartContractContext context,
            MappedState<string, long, Address, Hash> whitelistIdMap,
            WhitelistContractContainer.WhitelistContractReferenceState whitelistContract)
        {
            _context = context;
            _whitelistIdMap = whitelistIdMap;
            _whitelistContract = whitelistContract;
        }

        public void CreateWhitelist(CreateWhitelistInput input)
        {
            _whitelistContract.CreateWhitelist.Send(input);
        }

        public void AddExtraInfo(AddExtraInfoInput input)
        {
            
        }

        public void AddAddressInfoListToWhitelist(AddAddressInfoListToWhitelistInput input)
        {
            
        }

        public void RemoveAddressInfoListFromWhitelist(RemoveAddressInfoListFromWhitelistInput input)
        {
           
        }
        
    }
}
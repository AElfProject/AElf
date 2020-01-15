using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace AElf.Kernel.TransactionPool.Application
{
    public class ExtraAcceptedTokenUpdatedEventHandle : IBlockAcceptedLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IExtraAcceptedTokenService _extraAcceptedTokenService;
        private LogEvent _interestedEvent;
        private ILogger<ExtraAcceptedTokenUpdatedEventHandle> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new ExtraTokenListModified().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ExtraAcceptedTokenUpdatedEventHandle(ISmartContractAddressService smartContractAddressService,
            IExtraAcceptedTokenService extraAcceptedTokenService)
        {
            _smartContractAddressService = smartContractAddressService;
            _extraAcceptedTokenService = extraAcceptedTokenService;
            Logger = NullLogger<ExtraAcceptedTokenUpdatedEventHandle>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ExtraTokenListModified();
            eventData.MergeFrom(logEvent);
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            if (eventData.AllTokenInfos == null)
                return Task.CompletedTask;
            var newTokenInfoList = new List<AvailableTokenInfoInCache>();
            foreach (var tokenInfo in eventData.AllTokenInfos.AllAvailableTokens)
            {
                newTokenInfoList.Add(new AvailableTokenInfoInCache
                {
                    TokenSymbol = tokenInfo.TokenSymbol,
                    AddedTokenWeight = tokenInfo.AddedTokenWeight,
                    BaseTokenWeight = tokenInfo.BaseTokenWeight
                });
            }

            _extraAcceptedTokenService.SetExtraAcceptedTokenInfoToForkCache(blockIndex, newTokenInfoList);
            return Task.CompletedTask;
        }
    }
}
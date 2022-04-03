using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit.Managers;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Services
{
    internal class ProfitService : IProfitService
    {
        private readonly CSharpSmartContractContext _context;
        private readonly TokenContractContainer.TokenContractReferenceState _tokenContract;
        private readonly IBeneficiaryManager _beneficiaryManager;
        private readonly IProfitDetailManager _profitDetailManager;
        private readonly IProfitSchemeManager _profitSchemeManager;
        private readonly MappedState<Address, DistributedProfitsInfo> _distributedProfitsInfoMap;

        public ProfitService(CSharpSmartContractContext context,
            TokenContractContainer.TokenContractReferenceState tokenContract,
            IBeneficiaryManager beneficiaryManager, IProfitDetailManager profitDetailManager,
            IProfitSchemeManager profitSchemeManager,
            MappedState<Address, DistributedProfitsInfo> distributedProfitsInfoMap)
        {
            _context = context;
            _tokenContract = tokenContract;
            _beneficiaryManager = beneficiaryManager;
            _profitDetailManager = profitDetailManager;
            _profitSchemeManager = profitSchemeManager;
            _distributedProfitsInfoMap = distributedProfitsInfoMap;
        }

        public void Contribute(Hash schemeId, long period, string symbol, long amount)
        {
            AssertTokenExists(symbol);
            if (amount <= 0)
            {
                throw new AssertionException("Amount need to greater than 0.");
            }

            var scheme = _profitSchemeManager.GetScheme(schemeId);

            if (period == 0)
            {
                // Contribute to general ledger.
                _tokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = _context.Sender,
                    To = scheme.VirtualAddress,
                    Symbol = symbol,
                    Amount = amount,
                    Memo = $"Add {amount} dividends."
                });
            }
            else
            {
                if (period < scheme.CurrentPeriod)
                {
                    throw new AssertionException("Invalid contributing period.");
                }

                var distributedPeriodProfitsVirtualAddress =
                    GetDistributedPeriodProfitsVirtualAddress(schemeId, period);

                var distributedProfitsInformation = _distributedProfitsInfoMap[distributedPeriodProfitsVirtualAddress];
                if (distributedProfitsInformation == null)
                {
                    distributedProfitsInformation = new DistributedProfitsInfo
                    {
                        AmountsMap = { { symbol, amount } }
                    };
                }
                else
                {
                    if (distributedProfitsInformation.IsReleased)
                    {
                        throw new AssertionException($"Scheme of period {period} already released.");
                    }

                    distributedProfitsInformation.AmountsMap[symbol] =
                        distributedProfitsInformation.AmountsMap[symbol].Add(amount);
                }

                _tokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = _context.Sender,
                    To = distributedPeriodProfitsVirtualAddress,
                    Symbol = symbol,
                    Amount = amount,
                });

                _distributedProfitsInfoMap[distributedPeriodProfitsVirtualAddress] = distributedProfitsInformation;
            }

            // If someone directly use virtual address to do the contribution, won't sense the token symbol he was using.
            _profitSchemeManager.AddReceivedTokenSymbol(schemeId, symbol);
        }

        private void AssertTokenExists(string symbol)
        {
            if (string.IsNullOrEmpty(_tokenContract.GetTokenInfo.Call(new GetTokenInfoInput { Symbol = symbol })
                    .TokenName))
            {
                throw new AssertionException($"Token {symbol} not exists.");
            }
        }
        
        private Address GetDistributedPeriodProfitsVirtualAddress(Hash schemeId, long period)
        {
            return _context.ConvertVirtualAddressToContractAddress(
                GeneratePeriodVirtualAddressFromHash(schemeId, period));
        }

        private Hash GeneratePeriodVirtualAddressFromHash(Hash schemeId, long period)
        {
            return HashHelper.XorAndCompute(schemeId, HashHelper.ComputeFrom(period));
        }

        public void Distribute(Hash schemeId, long period, Dictionary<string, long> amountMap)
        {
            throw new System.NotImplementedException();
        }

        public void Claim(Hash schemeId, Address beneficiary)
        {
            throw new System.NotImplementedException();
        }

        public void Burn(Hash schemeId, long period, Dictionary<string, long> amountMap)
        {
            throw new System.NotImplementedException();
        }
    }
}
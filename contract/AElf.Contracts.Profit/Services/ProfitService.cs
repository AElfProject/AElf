using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit.Helpers;
using AElf.Contracts.Profit.Managers;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Profit.Services
{
    internal partial class ProfitService : IProfitService
    {
        private readonly CSharpSmartContractContext _context;
        private readonly TokenContractContainer.TokenContractReferenceState _tokenContract;
        private readonly IBeneficiaryManager _beneficiaryManager;
        private readonly IProfitDetailManager _profitDetailManager;
        private readonly IProfitSchemeManager _profitSchemeManager;
        private readonly IDistributedProfitsInfoManager _distributedProfitsInfoManager;

        public ProfitService(CSharpSmartContractContext context,
            TokenContractContainer.TokenContractReferenceState tokenContract,
            IBeneficiaryManager beneficiaryManager, IProfitDetailManager profitDetailManager,
            IProfitSchemeManager profitSchemeManager,
            IDistributedProfitsInfoManager distributedProfitsInfoManager)
        {
            _context = context;
            _tokenContract = tokenContract;
            _beneficiaryManager = beneficiaryManager;
            _profitDetailManager = profitDetailManager;
            _profitSchemeManager = profitSchemeManager;
            _distributedProfitsInfoManager = distributedProfitsInfoManager;
        }

        public void Contribute(Hash schemeId, long period, string symbol, long amount)
        {
            AssertTokenExists(symbol);
            if (amount <= 0)
            {
                throw new AssertionException("Amount need to greater than 0.");
            }

            var scheme = _profitSchemeManager.GetScheme(schemeId);

            Address toAddress;
            if (period == 0)
            {
                toAddress = scheme.VirtualAddress;
            }
            else
            {
                if (period < scheme.CurrentPeriod)
                {
                    throw new AssertionException("Invalid contributing period.");
                }

                var periodVirtualAddress = ProfitHelper.CalculatePeriodVirtualAddress(_context, schemeId, period);
                _distributedProfitsInfoManager.AddProfits(schemeId, period, symbol, amount);
                toAddress = periodVirtualAddress;
            }

            _tokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = _context.Sender,
                To = toAddress,
                Symbol = symbol,
                Amount = amount,
                Memo = $"Add {amount} dividends."
            });

            // If someone directly use virtual address to do the contribution, won't sense the token symbol he was using.
            _profitSchemeManager.AddReceivedTokenSymbol(schemeId, symbol);
        }

        public void Distribute(Hash schemeId, long period, Dictionary<string, long> amountMap)
        {
            var scheme = _profitSchemeManager.GetScheme(schemeId);

            // Permission Check.
            if (_context.Sender != scheme.Manager && _context.Sender !=
                _context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName))
            {
                throw new AssertionException("Only manager or token holder contract can distribute profits.");
            }

            var actualAmountMap = new Dictionary<string, long>();
            if (amountMap.Any())
            {
                // Follow the input.
                foreach (var pair in amountMap)
                {
                    var symbol = pair.Key;
                    var amount = pair.Value;
                    AssertTokenExists(symbol);
                    var actualAmount = pair.Value == 0
                        ? GetBalance(scheme.VirtualAddress, symbol)
                        : amount;
                    actualAmountMap.Add(symbol, actualAmount);
                }
            }
            else
            {
                if (scheme.IsReleaseAllBalanceEveryTimeByDefault && scheme.ReceivedTokenSymbols.Any())
                {
                    // Prepare to distribute all from general ledger.
                    foreach (var symbol in scheme.ReceivedTokenSymbols)
                    {
                        var balance = GetBalance(scheme.VirtualAddress, symbol);
                        actualAmountMap.Add(symbol, balance);
                    }
                }
            }

            var totalShares = scheme.TotalShares;

            // Set and load delay distribute cache.
            if (scheme.DelayDistributePeriodCount > 0)
            {
                if (scheme.CachedDelayTotalShares.ContainsKey(period))
                {
                    totalShares = scheme.CachedDelayTotalShares[period];
                    scheme.CachedDelayTotalShares.Remove(period);
                }
                else
                {
                    totalShares = 0;
                }

                var delayPeriod = period.Add(scheme.DelayDistributePeriodCount);
                if (scheme.CachedDelayTotalShares.ContainsKey(delayPeriod))
                {
                    scheme.CachedDelayTotalShares[delayPeriod] =
                        scheme.CachedDelayTotalShares[delayPeriod].Add(totalShares);
                }
                else
                {
                    scheme.CachedDelayTotalShares[delayPeriod] = totalShares;
                }
            }

            if (period != scheme.CurrentPeriod)
            {
                throw new AssertionException(
                    $"Invalid period. When release scheme {schemeId.ToHex()} of period {period}. Current period is {scheme.CurrentPeriod}");
            }

            var periodVirtualAddress =
                ProfitHelper.CalculatePeriodVirtualAddress(_context, schemeId, period);

            if (period < 0 || totalShares <= 0)
            {
                Burn(schemeId, period, actualAmountMap);
                return;
            }

            _context.LogDebug(() => $"Receiving virtual address: {periodVirtualAddress}");

            _profitSchemeManager.CacheDistributedPeriodTotalShares(schemeId, period, totalShares);
            _distributedProfitsInfoManager.MarkAsDistributed(schemeId, period, totalShares, actualAmountMap);

            PerformDistributeProfits(actualAmountMap, scheme, totalShares, periodVirtualAddress);

            _profitSchemeManager.MoveToNextPeriod(schemeId);
        }

        public void Claim(Hash schemeId, Address beneficiary)
        {
            var scheme = _profitSchemeManager.GetScheme(schemeId);

            var profitDetails = _profitDetailManager.GetProfitDetails(schemeId, beneficiary);
            if (profitDetails == null)
            {
                throw new AssertionException("Profit details not found.");
            }

            _context.LogDebug(
                () => $"{_context.Sender} is trying to profit from {schemeId.ToHex()} for {beneficiary}.");

            var profitableDetails =
                profitDetails.Details.Where(d => d.LastProfitPeriod < scheme.CurrentPeriod).ToList();

            _context.LogDebug(() =>
                $"Profitable details: {profitableDetails.Aggregate("\n", (profit1, profit2) => profit1.ToString() + "\n" + profit2)}");

            // Only can get profit from last profit period to actual last period (profit.CurrentPeriod - 1),
            // because current period not released yet.
            for (var i = 0;
                 i < Math.Min(ProfitContractConstants.ProfitReceivingLimitForEachTime, profitableDetails.Count);
                 i++)
            {
                var profitDetail = profitableDetails[i];
                if (profitDetail.LastProfitPeriod == 0)
                {
                    // This detail never performed profit before.
                    profitDetail.LastProfitPeriod = profitDetail.StartPeriod;
                }

                var claimableProfitList = ExtractClaimableProfitList(scheme, profitDetail);
                foreach (var claimableProfit in claimableProfitList)
                {
                    foreach (var pair in claimableProfit.AmountMap)
                    {
                        var symbol = pair.Key;
                        var amount = pair.Value;
                        if (_distributedProfitsInfoManager.GetDistributedProfitsInfo(schemeId, claimableProfit.Period)
                                .IsReleased && amount > 0)
                        {
                            _context.SendVirtualInline(
                                ProfitHelper.GeneratePeriodVirtualAddressFromHash(scheme.SchemeId,
                                    claimableProfit.Period),
                                _tokenContract.Value,
                                nameof(_tokenContract.Transfer), new TransferInput
                                {
                                    To = beneficiary,
                                    Symbol = symbol,
                                    Amount = amount
                                }.ToByteString());

                            _context.Fire(new ProfitsClaimed
                            {
                                Beneficiary = beneficiary,
                                Symbol = symbol,
                                Amount = amount,
                                ClaimerShares = claimableProfit.Shares,
                                TotalShares = claimableProfit.TotalShares,
                                Period = claimableProfit.Period
                            });
                        }
                    }
                }

                if (claimableProfitList.Any())
                {
                    _profitDetailManager.UpdateBeneficiaryProfitDetailLastProfitPeriod(schemeId, beneficiary,
                        profitDetail, claimableProfitList.Select(p => p.Period).Max().Add(1));
                }
            }

            var removedShares = _profitDetailManager.RemoveClaimedProfitDetails(schemeId, beneficiary);
            _profitSchemeManager.RemoveShares(schemeId, scheme.CurrentPeriod, removedShares);
        }

        public void Burn(Hash schemeId, long period, Dictionary<string, long> amountMap)
        {
            var scheme = _profitSchemeManager.GetScheme(schemeId);
            BurnProfits(scheme, period, amountMap);
        }

        public void FixProfitDetail(Hash schemeId, BeneficiaryShare beneficiaryShare, long startPeriod, long endPeriod,
            Hash profitDetailId)
        {
            _profitDetailManager.FixProfitDetail(schemeId, beneficiaryShare, startPeriod, endPeriod,
                profitDetailId);
        }
    }
}
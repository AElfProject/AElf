using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit.Models;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Profit.Services
{
    internal partial class ProfitService
    {
        private void AssertTokenExists(string symbol)
        {
            if (string.IsNullOrEmpty(_tokenContract.GetTokenInfo.Call(new GetTokenInfoInput { Symbol = symbol })
                    .TokenName))
            {
                throw new AssertionException($"Token {symbol} not exists.");
            }
        }

        private void PerformDistributeProfits(Dictionary<string, long> profitsMap, Scheme scheme, long totalShares,
            Address profitsReceivingVirtualAddress)
        {
            foreach (var profits in profitsMap)
            {
                var symbol = profits.Key;
                var amount = profits.Value;
                var remainAmount = DistributeProfitsForSubSchemes(symbol, amount, scheme, totalShares);
                _context.LogDebug(() => $"Distributing {remainAmount} {symbol} tokens.");
                // Transfer remain amount to individuals' receiving profits address.
                if (remainAmount != 0)
                {
                    _context.SendVirtualInline(scheme.SchemeId, _tokenContract.Value,
                        nameof(_tokenContract.Transfer), new TransferInput
                        {
                            To = profitsReceivingVirtualAddress,
                            Amount = remainAmount,
                            Symbol = symbol
                        }.ToByteString());
                }
            }
        }

        private long DistributeProfitsForSubSchemes(string symbol, long totalAmount, Scheme scheme, long totalShares)
        {
            _context.LogDebug(() => $"Sub schemes count: {scheme.SubSchemes.Count}");
            var remainAmount = totalAmount;
            foreach (var subScheme in scheme.SubSchemes)
            {
                _context.LogDebug(() => $"Releasing {subScheme.SchemeId}");

                // General ledger of this sub profit scheme.
                var subSchemeVirtualAddress = _context.ConvertVirtualAddressToContractAddress(subScheme.SchemeId);

                var distributeAmount = SafeCalculateProfits(totalAmount, subScheme.Shares, totalShares);
                if (distributeAmount != 0)
                {
                    _context.SendVirtualInline(scheme.SchemeId, _tokenContract.Value,
                        nameof(_tokenContract.Transfer), new TransferInput
                        {
                            To = subSchemeVirtualAddress,
                            Amount = distributeAmount,
                            Symbol = symbol
                        }.ToByteString());
                }

                remainAmount = remainAmount.Sub(distributeAmount);

                _profitDetailManager.UpdateSubSchemeProfitDetailLastProfitPeriod(scheme.SchemeId, subSchemeVirtualAddress,
                    scheme.CurrentPeriod);

                // Update sub scheme.
                _profitSchemeManager.AddReceivedTokenSymbol(subScheme.SchemeId, symbol);
            }

            return remainAmount;
        }

        private long GetBalance(Address address, string symbol)
        {
            return _tokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = address,
                Symbol = symbol
            }).Balance;
        }

        /// <summary>
        /// Just burn balance in general ledger.
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="period"></param>
        /// <param name="amountMap"></param>
        /// <returns></returns>
        private void BurnProfits(Scheme scheme, long period, Dictionary<string, long> amountMap)
        {
            _profitSchemeManager.MoveToNextPeriod(scheme.SchemeId);

            var actualAmountMap = new Dictionary<string, long>();
            foreach (var profits in amountMap)
            {
                var symbol = profits.Key;
                var amount = profits.Value;
                if (amount > 0)
                {
                    var balanceOfToken = _tokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = scheme.VirtualAddress,
                        Symbol = symbol
                    });
                    if (balanceOfToken.Balance < amount)
                        continue;
                    _context.SendVirtualInline(scheme.SchemeId, _tokenContract.Value,
                        nameof(_tokenContract.Transfer), new TransferInput
                        {
                            To = _context.Self,
                            Amount = amount,
                            Symbol = symbol
                        }.ToByteString());
                    _tokenContract.Burn.Send(new BurnInput
                    {
                        Amount = amount,
                        Symbol = symbol
                    });
                    actualAmountMap.Add(symbol, -amount);
                }
            }

            _distributedProfitsInfoManager.MarkAsDistributed(scheme.SchemeId, period, 0, actualAmountMap);
        }

        public List<ClaimableProfit> ExtractClaimableProfitList(Scheme scheme, ProfitDetail profitDetail,
            List<string> symbolList = null)
        {
            var claimableProfitList = new List<ClaimableProfit>();
            if (symbolList == null)
            {
                symbolList = scheme.ReceivedTokenSymbols.ToList();
            }

            long maxPeriod;
            if (profitDetail.EndPeriod == long.MaxValue)
            {
                var atMostProfitToPeriod = profitDetail.LastProfitPeriod.Add(ProfitContractConstants
                    .MaximumProfitReceivingPeriodCountOfOneTime);
                maxPeriod = Math.Min(scheme.CurrentPeriod.Sub(1), atMostProfitToPeriod);
            }
            else
            {
                maxPeriod = Math.Min(scheme.CurrentPeriod.Sub(1), profitDetail.EndPeriod);
            }

            for (var period = profitDetail.LastProfitPeriod; period <= maxPeriod; period++)
            {
                var claimableProfit = new ClaimableProfit
                {
                    SchemeId = scheme.SchemeId,
                    Period = period,
                    Shares = profitDetail.Shares,
                    AmountMap = new Dictionary<string, long>(),
                    TotalShares = scheme.TotalShares
                };
                var distributedProfitsInfo =
                    _distributedProfitsInfoManager.GetDistributedProfitsInfo(scheme.SchemeId, period);
                foreach (var symbol in symbolList)
                {
                    if (distributedProfitsInfo == null || distributedProfitsInfo.TotalShares == 0 ||
                        !distributedProfitsInfo.AmountsMap.Any() ||
                        !distributedProfitsInfo.AmountsMap.ContainsKey(symbol))
                    {
                        continue;
                    }

                    var amount = SafeCalculateProfits(distributedProfitsInfo.AmountsMap[symbol], profitDetail.Shares,
                        distributedProfitsInfo.TotalShares);
                    claimableProfit.AmountMap.Add(symbol, amount);
                }

                claimableProfitList.Add(claimableProfit);
            }

            return claimableProfitList;
        }

        private static long SafeCalculateProfits(long totalAmount, long shares, long totalShares)
        {
            var decimalTotalAmount = (decimal)totalAmount;
            var decimalShares = (decimal)shares;
            var decimalTotalShares = (decimal)totalShares;
            return (long)(decimalTotalAmount * decimalShares / decimalTotalShares);
        }
    }
}
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        private void CheckSenderNFTBalanceAndAllowance(string symbol, long tokenId, long quantity)
        {
            var balance = State.NFTContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Owner = Context.Sender
            }).Balance;
            Assert(balance >= quantity, "Check sender NFT balance failed.");
            var allowance = State.NFTContract.GetAllowance.Call(new GetAllowanceInput
            {
                Symbol = symbol,
                TokenId = tokenId,
                Owner = Context.Sender,
                Spender = Context.Self
            }).Allowance;
            Assert(allowance >= quantity, "Check sender NFT allowance failed.");
        }

        private void PerformDeal(PerformDealInput performDealInput)
        {
            var floorPrice = State.FloorPriceMap[performDealInput.NFTSymbol][performDealInput.PurchaseSymbol];
            Assert(performDealInput.PurchaseAmount >= floorPrice, "Deal price should be greater than the floor price.");

            var serviceFee = performDealInput.PurchaseAmount.Mul(State.ServiceFeeRate.Value).Div(FeeDenominator);
            var royalty = State.CertainNFTRoyaltyMap[performDealInput.NFTSymbol][performDealInput.NFTTokenId];
            if (royalty == 0)
            {
                royalty = State.RoyaltyMap[performDealInput.NFTSymbol];
            }

            var royaltyFee = performDealInput.PurchaseAmount.Mul(royalty).Div(FeeDenominator);
            var royaltyFeeReceiver = State.RoyaltyFeeReceiverMap[performDealInput.NFTSymbol];
            if (royaltyFeeReceiver == null)
            {
                royaltyFee = 0;
            }

            var actualAmount = performDealInput.PurchaseAmount.Sub(serviceFee).Sub(royaltyFee);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = performDealInput.NFTTo,
                To = performDealInput.NFTFrom,
                Symbol = performDealInput.PurchaseSymbol,
                Amount = actualAmount
            });
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = performDealInput.NFTTo,
                To = State.ServiceFeeReceiver.Value,
                Symbol = performDealInput.PurchaseSymbol,
                Amount = serviceFee
            });
            if (royaltyFeeReceiver != null)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = performDealInput.NFTTo,
                    To = royaltyFeeReceiver,
                    Symbol = performDealInput.PurchaseSymbol,
                    Amount = royaltyFee
                });
            }

            State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
            {
                From = performDealInput.NFTFrom,
                To = performDealInput.NFTTo,
                Symbol = performDealInput.NFTSymbol,
                TokenId = performDealInput.NFTTokenId,
                Amount = performDealInput.NFTQuantity
            });

            Context.Fire(new Sold
            {
                NftFrom = performDealInput.NFTFrom,
                NftTo = performDealInput.NFTTo,
                NftSymbol = performDealInput.NFTSymbol,
                NftTokenId = performDealInput.NFTTokenId,
                NftQuantity = performDealInput.NFTQuantity,
                PurchaseSymbol = performDealInput.PurchaseSymbol,
                PurchaseAmount = performDealInput.PurchaseAmount
            });
        }

        private struct PerformDealInput
        {
            public Address NFTFrom { get; set; }
            public Address NFTTo { get; set; }
            public string NFTSymbol { get; set; }
            public long NFTTokenId { get; set; }
            public long NFTQuantity { get; set; }
            public string PurchaseSymbol { get; set; }

            /// <summary>
            /// Be aware of that this stands for total amount.
            /// </summary>
            public long PurchaseAmount { get; set; }
        }

        private StringList GetTokenWhiteList(string symbol)
        {
            return State.TokenWhiteListMap[symbol] ?? new StringList
            {
                Value = {Context.Variables.NativeSymbol}
            };
        }

        private void PerformRequestNewItem(string symbol, long tokenId)
        {
            var customizeInfo = State.CustomizeInfoMap[symbol];
            if (customizeInfo == null)
            {
                throw new AssertionException("Cannot request new item for this protocol.");
            }

            Assert(State.RequestInfoMap[symbol][tokenId] == null, "Request already existed.");

            var virtualAddressFrom = CalculateTokenHash(symbol, tokenId);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.ConvertVirtualAddressToContractAddress(virtualAddressFrom),
                Symbol = customizeInfo.Symbol,
                Amount = customizeInfo.Deposit
            });

            State.RequestInfoMap[symbol][tokenId] = new RequestInfo
            {
                Symbol = symbol,
                TokenId = tokenId,
                Deposit = customizeInfo.Deposit,
                Balance = customizeInfo.Balance,
                WhiteListHours = customizeInfo.WhiteListHours,
                WorkHours = customizeInfo.WorkHours,
                Requester = Context.Sender
            };

            Context.Fire(new NewNFTRequested
            {
                Symbol = symbol,
                Requester = Context.Sender,
                TokenId = tokenId
            });
        }

        private bool CanBeListedWithAuction(string symbol, long tokenId)
        {
            var requestInfo = State.RequestInfoMap[symbol][tokenId];
            if (requestInfo == null)
            {
                return true;
            }

            var whiteListDueTime1 = requestInfo.ConfirmTime.AddHours(requestInfo.WorkHours)
                .AddHours(requestInfo.WhiteListHours);
            var whiteListDueTime2 = requestInfo.ListTime.AddHours(requestInfo.WhiteListHours);
            if (Context.CurrentBlockTime > whiteListDueTime1 && Context.CurrentBlockTime > whiteListDueTime2)
            {
                return true;
            }

            return false;
        }
        
        private Hash CalculateTokenHash(string symbol, long tokenId)
        {
            return HashHelper.ComputeFrom($"{symbol}{tokenId}");
        }
    }
}
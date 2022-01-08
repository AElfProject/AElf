using System;
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using GetBalanceInput = AElf.Contracts.MultiToken.GetBalanceInput;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract
    {
        public override Empty SetRoyalty(SetRoyaltyInput input)
        {
            // 0% - 10%
            Assert(0 <= input.Royalty && input.Royalty <= 1000);
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            if (input.TokenId == 0)
            {
                Assert(nftProtocolInfo.Creator == Context.Sender,
                    "Only NFT Protocol Creator can set royalty for whole protocol.");
                // Set for whole NFT Protocol.
                State.RoyaltyMap[input.Symbol] = input.Royalty;
            }
            else
            {
                var nftInfo = State.NFTContract.GetNFTInfo.Call(new GetNFTInfoInput
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId
                });
                Assert(nftProtocolInfo.Creator == Context.Sender || nftInfo.Minters.Contains(Context.Sender),
                    "No permission.");
                State.CertainNFTRoyaltyMap[input.Symbol][input.TokenId] = input.Royalty;
            }

            State.RoyaltyFeeReceiverMap[input.Symbol] = input.RoyaltyFeeReceiver;
            return new Empty();
        }

        public override Empty SetTokenWhiteList(SetTokenWhiteListInput input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can set token white list.");
            foreach (var symbol in State.GlobalTokenWhiteList.Value.Value)
            {
                if (!input.TokenWhiteList.Value.Contains(symbol))
                {
                    input.TokenWhiteList.Value.Add(symbol);
                }
            }

            State.TokenWhiteListMap[input.Symbol] = input.TokenWhiteList;
            return new Empty();
        }

        public override Empty SetCustomizeInfo(CustomizeInfo input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can set customize info.");
            if (input.StakingAmount > 0)
            {
                var virtualAddress = CalculateNFTVirtuaAddress(input.Symbol);
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = virtualAddress,
                    Symbol = input.Price.Symbol,
                    Amount = input.StakingAmount
                });
            }
            State.CustomizeInfoMap[input.Symbol] = input;
            return new Empty();
        }

        public override Empty StakeForRequests(StakeForRequestsInput input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can stake for requests.");
            var customizeInfo = State.CustomizeInfoMap[input.Symbol];
            var virtualAddress = CalculateNFTVirtuaAddress(input.Symbol);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = virtualAddress,
                Symbol = customizeInfo.Price.Symbol,
                Amount = input.StakingAmount
            });
            customizeInfo.StakingAmount = customizeInfo.StakingAmount.Add(input.StakingAmount);
            State.CustomizeInfoMap[input.Symbol] = customizeInfo;
            return new Empty();
        }

        public override Empty WithdrawStakingTokens(WithdrawStakingTokensInput input)
        {
            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can withdraw.");
            var customizeInfo = State.CustomizeInfoMap[input.Symbol];
            Assert(input.WithdrawAmount <= customizeInfo.StakingAmount, "Insufficient staking amount.");
            var virtualAddress = CalculateNFTVirtuaAddress(input.Symbol);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = virtualAddress,
                To = Context.Sender,
                Symbol = customizeInfo.Price.Symbol,
                Amount = input.WithdrawAmount
            });
            customizeInfo.StakingAmount = customizeInfo.StakingAmount.Sub(input.WithdrawAmount);
            State.CustomizeInfoMap[input.Symbol] = customizeInfo;
            return new Empty();
        }

        public override Empty HandleRequest(HandleRequestInput input)
        {
            var requestInfo = State.RequestInfoMap[input.Symbol][input.TokenId];
            if (requestInfo == null)
            {
                throw new AssertionException("Request not exists.");
            }

            var nftProtocolInfo = State.NFTContract.GetNFTProtocolInfo.Call((new StringValue {Value = input.Symbol}));
            Assert(nftProtocolInfo.Creator == Context.Sender, "Only NFT Protocol Creator can handle request.");

            var nftVirtualAddressFrom = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftVirtualAddress = Context.ConvertVirtualAddressToContractAddress(nftVirtualAddressFrom);
            var nftVirtualAddressBalance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = requestInfo.Price.Symbol,
                Owner = nftVirtualAddress
            }).Balance;

            if (input.IsConfirm)
            {
                requestInfo.IsConfirmed = true;
                requestInfo.ConfirmTime = Context.CurrentBlockTime;
                requestInfo.WorkHours = Math.Min(requestInfo.WorkHoursFromCustomizeInfo,
                    (requestInfo.DueTime - Context.CurrentBlockTime).Seconds.Div(3600));
                State.RequestInfoMap[input.Symbol][input.TokenId] = requestInfo;

                var transferAmount = nftVirtualAddressBalance.Mul(DefaultDepositConfirmRate).Div(FeeDenominator);
                var serviceFee = transferAmount.Mul(State.ServiceFeeRate.Value).Div(FeeDenominator);
                transferAmount = transferAmount.Sub(serviceFee);

                State.TokenContract.Transfer.VirtualSend(nftVirtualAddressFrom, new TransferInput
                {
                    To = Context.Sender,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = transferAmount
                });
                State.TokenContract.Transfer.VirtualSend(nftVirtualAddressFrom, new TransferInput
                {
                    To = State.ServiceFeeReceiver.Value,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = serviceFee
                });
                Context.Fire(new NewNFTRequestConfirmed
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Requester = input.Requester
                });
            }
            else
            {
                State.RequestInfoMap[input.Symbol].Remove(input.TokenId);
                State.TokenContract.Transfer.VirtualSend(nftVirtualAddressFrom, new TransferInput
                {
                    To = Context.Sender,
                    Symbol = requestInfo.Price.Symbol,
                    Amount = nftVirtualAddressBalance
                });
                Context.Fire(new NewNFTRequestRejected
                {
                    Symbol = input.Symbol,
                    TokenId = input.TokenId,
                    Requester = input.Requester
                });
            }

            return new Empty();
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.NFT;
using AElf.Contracts.NFTMarket.Helpers;
using AElf.Contracts.NFTMarket.Managers;
using AElf.Contracts.Whitelist;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket.Services;

internal class MakeOfferService : IMakeOfferService
{
    private readonly NFTContractContainer.NFTContractReferenceState _nftContract;
    private readonly WhitelistContractContainer.WhitelistContractReferenceState _whitelistContract;
    private readonly MappedState<Hash, Hash> _whitelistIdMap;
    private readonly MappedState<string, long, Address, ListedNFTInfoList> _listedNFTInfoListMap;
    private readonly IWhitelistManager _whitelistManager;
    private readonly CSharpSmartContractContext _context;

    public MakeOfferService(NFTContractContainer.NFTContractReferenceState nftContract,
        MappedState<Hash, Hash> whitelistIdMap,
        MappedState<string, long, Address, ListedNFTInfoList> listedNFTInfoListMap,
        IWhitelistManager whitelistManager,
        CSharpSmartContractContext context)
    {
        _nftContract = nftContract;
        _whitelistIdMap = whitelistIdMap;
        _listedNFTInfoListMap = listedNFTInfoListMap;
        _whitelistManager = whitelistManager;
        _context = context;
    }

    public void ValidateOffer(MakeOfferInput makeOfferInput)
    {
        if (_context.Sender == makeOfferInput.OfferTo)
        {
            throw new AssertionException("Origin owner cannot be sender himself.");
        }
    }

    public bool IsSenderInWhitelist(MakeOfferInput makeOfferInput,out Hash whitelistId)
    {
        var projectId =
            WhitelistHelper.CalculateProjectId(makeOfferInput.Symbol, makeOfferInput.TokenId, makeOfferInput.OfferTo);
        whitelistId = _whitelistIdMap[projectId];
        return whitelistId != null && _whitelistManager.IsAddressInWhitelist(_context.Sender, whitelistId);
    }

    public DealStatus GetDealStatus(MakeOfferInput makeOfferInput, out List<ListedNFTInfo> affordableNftInfoList)
    {
        affordableNftInfoList = new List<ListedNFTInfo>();
        var nftInfo = _nftContract.GetNFTInfo.Call(new GetNFTInfoInput
        {
            Symbol = makeOfferInput.Symbol,
            TokenId = makeOfferInput.TokenId
        });
        var protocolInfo = _nftContract.GetNFTProtocolInfo.Call(new StringValue { Value = makeOfferInput.Symbol });
        if (nftInfo.Quantity == 0 && !protocolInfo.IsTokenIdReuse && makeOfferInput.Quantity == 1)
        {
            // NFT not minted.
            return DealStatus.NFTNotMined;
        }

        if (nftInfo.Quantity <= 0)
        {
            throw new AssertionException("NFT does not exist.");
        }

        var listedNftInfoList =
            _listedNFTInfoListMap[makeOfferInput.Symbol][makeOfferInput.TokenId][
                makeOfferInput.OfferTo ?? nftInfo.Creator];

        if (listedNftInfoList == null || listedNftInfoList.Value.All(i => i.ListType == ListType.NotListed))
        {
            // NFT not listed.
            return DealStatus.NotDeal;
        }

        affordableNftInfoList = GetAffordableNftInfoList(makeOfferInput, listedNftInfoList);
        if (!affordableNftInfoList.Any())
        {
            return DealStatus.NotDeal;
        }

        if (affordableNftInfoList.Count == 1 || affordableNftInfoList.First().Quantity >= makeOfferInput.Quantity)
        {
            return DealStatus.DealWithOnePrice;
        }

        return DealStatus.DealWithMultiPrice;
    }

    private List<ListedNFTInfo> GetAffordableNftInfoList(MakeOfferInput makeOfferInput,
        ListedNFTInfoList listedNftInfoList)
    {
        return listedNftInfoList.Value.Where(i =>
            (i.Price.Symbol == makeOfferInput.Price.Symbol && i.Price.Amount <= makeOfferInput.Price.Amount ||
             i.ListType != ListType.FixedPrice) &&
            !IsListedNftTimedOut(i)).OrderBy(i => i.Price.Amount).ToList();
    }
    

    private bool IsListedNftTimedOut(ListedNFTInfo listedNftInfo)
    {
        var expireTime = listedNftInfo.Duration.StartTime.AddHours(listedNftInfo.Duration.DurationHours);
        return _context.CurrentBlockTime > expireTime;
    }
}
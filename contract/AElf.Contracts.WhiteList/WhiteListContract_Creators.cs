using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract : WhiteListContractContainer.WhiteListContractBase
    {
        public override Hash CreateWhiteList(CreateWhiteListInput input)
        {
            Assert(input.AddressList != null,"The whiteList address is null");
            var whiteListHash = CalculateWhiteListHash(input.AddressList);
            Assert(State.WhiteListInfoMap[whiteListHash] == null,"WhiteList already exists.");
            var whiteListInfo = new WhiteListInfo
            {
                WhiteListId = whiteListHash,
                AddressList = input.AddressList,
                IsAvailable = true,
                Remark = input.Remark
            };
            State.WhiteListInfoMap[whiteListHash] = whiteListInfo;
            Context.Fire(new WhiteListCreated
            {
                WhiteListId = whiteListHash,
                AddressList = whiteListInfo.AddressList,
                IsAvailable = whiteListInfo.IsAvailable,
                Remark = whiteListInfo.Remark
            });
            return whiteListHash;
        }

        public override Empty AddAddressToWhiteList(AddAddressToWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhiteListId);
            whiteListInfo.AddressList.Value.Add(input.Address);
            State.WhiteListInfoMap[whiteListInfo.WhiteListId] = whiteListInfo;
            Context.Fire(new WhiteListAddressAdded
            {
                WhiteListId = whiteListInfo.WhiteListId,
                AddressList = whiteListInfo.AddressList
            });
            return new Empty();
        }
        
        public override Empty RemoveAddressFromWhiteList(RemoveAddressFromWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhiteListId);
            Assert(whiteListInfo.AddressList.Value.Contains(input.Address),"Address doesn't exist.");
            whiteListInfo.AddressList.Value.Remove(input.Address);
            State.WhiteListInfoMap[whiteListInfo.WhiteListId] = whiteListInfo;
            Context.Fire(new WhiteListAddressRemoved()
            {
                WhiteListId = whiteListInfo.WhiteListId,
                AddressList = whiteListInfo.AddressList
            });
            return new Empty();
        }
        
        public override Empty AddAddressListToWhiteList(AddAddressListToWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhiteListId);
            whiteListInfo.AddressList.Value.AddRange(input.AddressList.Value);
            State.WhiteListInfoMap[whiteListInfo.WhiteListId] = whiteListInfo;
            Context.Fire(new WhiteListAddressAdded
            {
                WhiteListId = whiteListInfo.WhiteListId,
                AddressList = whiteListInfo.AddressList
            });
            return new Empty();
        }
        
        public override Empty RemoveAddressListFromWhiteList(RemoveAddressListFromWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhiteListId);
            foreach (var address in input.AddressList.Value)
            {
                Assert(whiteListInfo.AddressList.Value.Contains(address),"Address doesn't exist.");
                whiteListInfo.AddressList.Value.Remove(address);
            }
            State.WhiteListInfoMap[whiteListInfo.WhiteListId] = whiteListInfo;
            Context.Fire(new WhiteListAddressRemoved()
            {
                WhiteListId = whiteListInfo.WhiteListId,
                AddressList = whiteListInfo.AddressList
            });
            return new Empty();
        }

        public override Empty DisableWhiteList(DisableWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhiteListId);
            whiteListInfo.IsAvailable = false;
            State.WhiteListInfoMap[whiteListInfo.WhiteListId] = whiteListInfo;
            Context.Fire(new WhiteListDisabled
            {
                WhiteListId = whiteListInfo.WhiteListId,
                IsAvailable = whiteListInfo.IsAvailable,
                Remark = whiteListInfo.Remark
            });
            return new Empty();
        }

        
    }
    
}

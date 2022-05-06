using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract
    {
        public override Hash SubscribeWhiteList(SubscribeWhiteListInput input)
        {
            var whiteListInfo = AssertWhiteListInfo(input.WhiteListId);
            var subscribeId = CalculateSubscribeWhiteListHash($"{input.ProjectId}{input.WhiteListId}");
            Assert(State.SubscribeWhiteListInfoMap[subscribeId] == null,"Subscribe info already exist.");
            var subscribeWhiteListInfo = new SubscribeWhiteListInfo
            {
                SubscribeId = subscribeId,
                ProjectId = input.ProjectId,
                WhiteListId = whiteListInfo.WhiteListId,
                AddressList = whiteListInfo.AddressList,
                CustomizeInfo = input.CustomizeInfo,
                IsAvailable = true
            };
            State.SubscribeWhiteListInfoMap[subscribeId] = subscribeWhiteListInfo;
            Context.Fire(new WhiteListSubscribed()
            {
                SubscribeId = subscribeId,
                ProjectId = subscribeWhiteListInfo.ProjectId,
                WhiteListId = subscribeWhiteListInfo.WhiteListId,
                AddressList = subscribeWhiteListInfo.AddressList,
                CustomizeInfo = subscribeWhiteListInfo.CustomizeInfo,
                IsAvailable = subscribeWhiteListInfo.IsAvailable
            });
            return subscribeId;
        }
    }
}
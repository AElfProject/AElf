using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NFTMinter;

public partial class NFTMinterContractState : ContractState
{
    public SingletonState<Address> AdminAddress { get; set; }

    /// <summary>
    ///     Symbol -> Token Id -> BadgeInfo
    /// </summary>
    public MappedState<string, long, BadgeInfo> BadgeInfoMap { get; set; }

    /// <summary>
    ///     Symbol -> Token Id -> Limit
    /// </summary>
    public MappedState<string, long, long> MintLimitMap { get; set; }

    /// <summary>
    ///     Symbol -> Token Id -> Minted Count
    /// </summary>
    public MappedState<string, long, long> MintedMap { get; set; }

    /// <summary>
    ///     Symbol -> Token Id -> Address -> Is in white list
    /// </summary>
    public MappedState<string, long, Address, bool> IsInWhiteListMap { get; set; }

    public MappedState<string, int, BlindBoxInfo> BlindBoxInfoMap { get; set; }
    public MappedState<string, int, Int64List> BlindBoxWeightVectorMap { get; set; }
}
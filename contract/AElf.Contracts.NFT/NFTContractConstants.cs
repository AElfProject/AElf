namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        private const int NumberMinLength = 10;
        private const int NumberMaxLength = 20;

        private const string NftTypeMetadataKey = "aelf_nft_type";
        private const string NftBaseUriMetadataKey = "aelf_nft_base_uri";
        private const string NftTokenIdReuseMetadataKey = "aelf_nft_token_id_reuse";
        private const string NftTotalSupplyMetadataKey = "aelf_nft_total_supply";
        private const string AssembledNftsKey = "aelf_assembled_nfts";
        private const string AssembledFtsKey = "aelf_assembled_fts";
    }
}
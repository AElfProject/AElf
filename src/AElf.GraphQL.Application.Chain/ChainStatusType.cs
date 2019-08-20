using AElf.Dtos;
using GraphQL.Types;

namespace AElf.GraphQL.Application.Chain
{
    public class ChainStatusType : ObjectGraphType<ChainStatusDto>
    {
        public ChainStatusType()
        {
            Field(c => c.ChainId);
            Field(c => c.BestChainHash);
            Field(c => c.BestChainHeight);
            Field(c => c.LastIrreversibleBlockHash);
            Field(c => c.LastIrreversibleBlockHeight);
        }
    }

    public class ChainStatusQuery : ObjectGraphType
    {
        public ChainStatusQuery(IChainStatusRepository chainStatusRepository)
        {
            Field<ChainStatusType>("chain_status", resolve: context => chainStatusRepository.GetChainStatus());
        }
    }
}
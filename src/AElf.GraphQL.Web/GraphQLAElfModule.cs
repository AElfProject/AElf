using System;
using AElf.GraphQL.Application.Chain;
using AElf.GraphQL.Application.Core;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.GraphQL.Web
{
    [DependsOn(typeof(ChainApplicationGraphQLAElfModule))]
    public class GraphQLAElfModule : AElfModule
    {
        
    }
}
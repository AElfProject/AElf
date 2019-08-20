using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace AElf.GraphQL.Application.Chain
{
    // ReSharper disable once InconsistentNaming
    public class AElfGraphQLMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IChainStatusRepository _chainStatusRepository;

        public AElfGraphQLMiddleware(RequestDelegate next, IChainStatusRepository chainStatusRepository)
        {
            _next = next;
            _chainStatusRepository = chainStatusRepository;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/graphql"))
            {
                using (var stream = new StreamReader(httpContext.Request.Body))
                {
                    var query = await stream.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        var schema = new Schema {Query = new ChainStatusQuery(_chainStatusRepository)};
                        var result = await new DocumentExecuter().ExecuteAsync(option =>
                        {
                            option.Schema = schema;
                            option.Query = query;
                        });
                        await WriteResultAsync(httpContext, result);
                    }
                }
            }
            else
            {
                await _next(httpContext);
            }
        }

        private async Task WriteResultAsync(HttpContext httpContext, ExecutionResult result)
        {
            var json = new DocumentWriter(indent: true).Write(result);
            httpContext.Response.StatusCode = 200;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(json);
        }
    }
}
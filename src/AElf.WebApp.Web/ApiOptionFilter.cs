using System;
using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AElf.WebApp.Web
{
    /// <summary>
    /// To hide api of ABP.
    /// </summary>
    internal class ApiOptionFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var apiDescription in context.ApiDescriptions)
            {
                if (apiDescription.TryGetMethodInfo(out MethodInfo method))
                {
                    if (method.ReflectedType != null && method.ReflectedType.CustomAttributes.Any())
                    {
                        var pathToRemove = swaggerDoc.Paths
                            .Where(p => p.Key.Contains("Abp", StringComparison.CurrentCultureIgnoreCase)).ToList();
                        foreach (var item in pathToRemove)
                        {
                            swaggerDoc.Paths.Remove(item.Key);
                        }
                    }
                }
            }
        }
    }
}
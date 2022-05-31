using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.Conventions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Http;

namespace AElf.WebApp.Web;

public class AElfConventionalRouteBuilder : ConventionalRouteBuilder
{
    protected AbpConventionalControllerOptions Options { get; }

    public AElfConventionalRouteBuilder(IOptions<AbpConventionalControllerOptions> options) : base(options)
    {
        Options = options.Value;
    }

    public override string Build(string rootPath, string controllerName, ActionModel action, string httpMethod,
        ConventionalControllerSetting configuration)
    {
        var controllerNameInUrl = NormalizeUrlControllerName(rootPath, controllerName, action, httpMethod, configuration);

        var url = $"api/{controllerNameInUrl.ToCamelCase()}";

        //Add {id} path if needed
        if (action.Parameters.Any(p => p.ParameterName == "id"))
        {
            url += "/{id}";
        }

        //Add action name if needed
        var actionNameInUrl = NormalizeUrlActionName(rootPath, controllerName, action, httpMethod, configuration);
        if (!actionNameInUrl.IsNullOrEmpty())
        {
            url += $"/{actionNameInUrl.ToCamelCase()}";
        }

        return url;
    }
}
using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.Conventions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Http;

namespace AElf.WebApp.Web;

public class ConventionalRouteBuilder : IConventionalRouteBuilder, ITransientDependency
{
    protected AbpConventionalControllerOptions Options { get; }

    public ConventionalRouteBuilder(IOptions<AbpConventionalControllerOptions> options)
    {
        Options = options.Value;
    }

    public string Build(string rootPath, string controllerName, ActionModel action, string httpMethod,
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

    protected virtual string NormalizeUrlActionName(string rootPath, string controllerName, ActionModel action,
        string httpMethod, [CanBeNull] ConventionalControllerSetting configuration)
    {
        var actionNameInUrl = HttpMethodHelper
            .RemoveHttpMethodPrefix(action.ActionName, httpMethod)
            .RemovePostFix("Async");

        if (configuration?.UrlActionNameNormalizer == null)
        {
            return actionNameInUrl;
        }

        return configuration.UrlActionNameNormalizer(
            new UrlActionNameNormalizerContext(
                rootPath,
                controllerName,
                action,
                actionNameInUrl,
                httpMethod
            )
        );
    }

    protected virtual string NormalizeUrlControllerName(string rootPath, string controllerName, ActionModel action,
        string httpMethod, [CanBeNull] ConventionalControllerSetting configuration)
    {
        if (configuration?.UrlControllerNameNormalizer == null)
        {
            return controllerName;
        }

        return configuration.UrlControllerNameNormalizer(
            new UrlControllerNameNormalizerContext(
                rootPath,
                controllerName
            )
        );
    }

    protected virtual string NormalizeControllerNameCase(string controllerName, [CanBeNull] ConventionalControllerSetting configuration)
    {
        if (configuration?.UseV3UrlStyle ?? Options.UseV3UrlStyle)
        {
            return controllerName.ToCamelCase();
        }

        return controllerName.ToKebabCase();
    }
}
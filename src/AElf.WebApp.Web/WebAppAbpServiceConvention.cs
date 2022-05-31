using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Conventions;

namespace AElf.WebApp.Web;

public class WebAppAbpServiceConvention : AbpServiceConvention
{
    public WebAppAbpServiceConvention(IOptions<AbpAspNetCoreMvcOptions> options,
        IConventionalRouteBuilder conventionalRouteBuilder) : base(options, conventionalRouteBuilder)
    {

    }
}
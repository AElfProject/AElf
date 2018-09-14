using System;
using AElf.Configuration.Config.Management;
using AElf.Cryptography;
using AElf.Management.Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AElf.Management.Website
{
    public class AuthenticationFilter: IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!DeployConfig.Instance.Authentication)
            {
                return;
            }

            var signType = context.HttpContext.Request.Headers["auth-type"];

            if (signType == "apikey")
            {
                var sign = context.HttpContext.Request.Headers["sign"];
                var chainId = context.ActionArguments["chainId"].ToString();
                var method = context.HttpContext.Request.Method;
                var timestamp = context.HttpContext.Request.Headers["timestamp"];

                if (ApiAuthenticationHelper.Check(ApiKeyConfig.Instance.ChainKeys[chainId], chainId, method, timestamp, sign, DeployConfig.Instance.SignTimeout))
                {
                    return;
                }

                context.Result = new JsonResult(new ApiEmptyResult(401, "Unauthorized"));
            }
            else
            {
                throw new Exception();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Modularity;
using AElf.WebApp.Application;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using System.Collections.Generic;
using System.Net.Http.Headers;
using AElf.WebApp.Application.Chain;
using AElf.WebApp.Application.Kernel;
using AElf.WebApp.Application.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;

namespace AElf.WebApp.Web
{
    [DependsOn(
        typeof(ChainApplicationWebAppAElfModule), 
        typeof(NetApplicationWebAppAElfModule), 
        typeof(AbpAspNetCoreMvcModule))]
    public class WebWebAppAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //var hostingEnvironment = context.Services.GetHostingEnvironment();
            //var configuration = context.Services.GetConfiguration();

            ConfigureAutoApiControllers();
            ConfigureSwaggerServices(context.Services);


            /*services.Configure<MvcOptions>(options =>
            {
                options.InputFormatters.Add(new ProtobufInputFormatter());
                options.OutputFormatters.Add(new ProtobufOutputFormatter());
            });*/
        }

        private void ConfigureAutoApiControllers()
        {
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {

                options.ConventionalControllers.Create(typeof(ChainApplicationWebAppAElfModule).Assembly, settings =>
                {
                    settings.RootPath = "chain";
                    settings.ApiVersions.Add(new ApiVersion(1, 0));
                });
                
                options.ConventionalControllers.Create(typeof(NetApplicationWebAppAElfModule).Assembly, settings =>
                {
                    settings.RootPath = "net";
                    settings.ApiVersions.Add(new ApiVersion(1, 0));
                });
            });
        }

        private void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(
                options =>
                {
                    options.SwaggerDoc("v1", new Info {Title = "AELF API", Version = "v1"});
                    options.DocInclusionPredicate((docName, description) => true);
                    options.CustomSchemaIds(type => type.FullName);
                });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            //var env = context.GetEnvironment();

            /*if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseErrorPage();
            }*/

            //app.UseVirtualFiles();
            //app.UseAuthentication();

            //app.UseAbpRequestLocalization();

            app.UseSwagger();
            app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore API"); });


            app.UseMvc(routes => { });
        }
    }


    // Thanks to https://tero.teelahti.fi/using-google-proto3-with-aspnet-mvc/
    // The input formatter reading request body and mapping it to given data object.
    /*public class ProtobufInputFormatter : InputFormatter
    {
        static MediaTypeHeaderValue protoMediaType = MediaTypeHeaderValue.Parse("application/x-protobuf");

        public override bool CanRead(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            MediaTypeHeaderValue requestContentType = null;
            MediaTypeHeaderValue.TryParse(request.ContentType, out requestContentType);

            if (requestContentType == null)
            {
                return false;
            }

            return requestContentType.IsSubsetOf(protoMediaType);
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            try
            {
                var request = context.HttpContext.Request;
                var obj = (IMessage) Activator.CreateInstance(context.ModelType);
                obj.MergeFrom(request.Body);

                return InputFormatterResult.SuccessAsync(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
                return InputFormatterResult.FailureAsync();
            }
        }
    }

// The output object mapping returned object to Protobuf-serialized response body.
    public class ProtobufOutputFormatter : OutputFormatter
    {
        static MediaTypeHeaderValue protoMediaType = MediaTypeHeaderValue.Parse("application/x-protobuf");

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            StringSegment seg;
            if (context.Object == null || !context.ContentType.IsSubsetOf(protoMediaType))
            {
                return false;
            }

            // Check whether the given object is a proto-generated object
            return context.ObjectType.GetTypeInfo()
                .ImplementedInterfaces
                .Where(i => i.GetTypeInfo().IsGenericType)
                .Any(i => i.GetGenericTypeDefinition() == typeof(IMessage<>));
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;

            // Proto-encode
            var protoObj = context.Object as IMessage;
            var serialized = protoObj.ToByteArray();

            return response.Body.WriteAsync(serialized, 0, serialized.Length);
        }
    }*/
}
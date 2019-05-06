using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using AElf.WebApp.Application.Chain;
using AElf.WebApp.Application.Net;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AElf.WebApp.Web
{
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
       
        private readonly IApiVersionDescriptionProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        public ConfigureSwaggerOptions( IApiVersionDescriptionProvider provider ) => _provider = provider;

        /// <inheritdoc />
        public void Configure( SwaggerGenOptions options )
        {
            // add a swagger document for each discovered API version
            // note: you might choose to skip or document deprecated API versions differently
            foreach ( var description in _provider.ApiVersionDescriptions )
            {
                options.SwaggerDoc( description.GroupName, CreateInfoForApiVersion( description ) );
            }
            options.IncludeXmlComments(() =>
            {
                var webApiDocument = XDocument.Load(Path.Combine(typeof(ChainApplicationWebAppAElfModule).Assembly.Location.Replace(".dll", ".xml")));
                webApiDocument.Root?.Add(XDocument.Load(Path.Combine(typeof(NetApplicationWebAppAElfModule).Assembly.Location.Replace(".dll", ".xml"))).Root?.Elements());
                return new XPathDocument(webApiDocument.CreateReader());
            });
        }

        static Info CreateInfoForApiVersion( ApiVersionDescription description )
        {
            var info = new Info()
            {
                Title = $"AELF API {description.ApiVersion.ToString()}",
                Version = description.ApiVersion.ToString()
            };

            if ( description.IsDeprecated )
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Heimdall.Api.Diagnostics
{
    public static class EndpointLoggingExtensions
    {
        public static void LogFileServing(this IApplicationBuilder app, string filePath)
        {
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                var log = logger?.CreateLogger("FileServingLogger");
                log?.LogInformation($"Tentando servir arquivo: {filePath} | Existe: {File.Exists(filePath)}");
                await next(); 
            });
        }
    }
}

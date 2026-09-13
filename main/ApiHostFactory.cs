using Alissa.Core.Api;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;

namespace Alissa.Main
{
    /// <summary>
    /// Factory for creating and managing the ASP.NET Core API host.
    /// </summary>
    internal static class ApiHostFactory
    {
        /// <summary>
        /// Creates and starts an ASP.NET Core API host if --api-port is provided.
        /// Returns a task that runs the host; returns completed task if API is disabled.
        /// </summary>
        public static Task CreateAndStartApiHostAsync(
            AppConfig config,
            string basePath,
            ICodeIntegrationService codeIntegrationService,
            ICommandService commandService)
        {
            bool apiEnabled = config.Api.Enabled;
            bool hasApiPort = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ALISSA_API_PORT"));

            if (!apiEnabled && !hasApiPort)
            {
                return Task.CompletedTask;
            }

            try
            {
                var builder = WebApplication.CreateBuilder();

                // Configure server to listen on the configured port
                int port = config.Api.ListenPort;
                bool hasEnvPort = int.TryParse(
                    Environment.GetEnvironmentVariable("ALISSA_API_PORT"),
                    out int envPort);

                if (hasEnvPort)
                {
                    port = envPort;
                }

                builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

                // Build app
                var app = builder.Build();

                // Add API authentication middleware
                app.UseApiAuthentication(config.Api);

                // Map endpoints
                app.MapCodeEndpoints(codeIntegrationService, basePath);
                app.MapCommandEndpoints(commandService);

                // Add health check
                app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

                // Start host in background
                return app.RunAsync($"http://127.0.0.1:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to start HTTP host: {ex.Message}");
                return Task.CompletedTask;
            }
        }
    }
}

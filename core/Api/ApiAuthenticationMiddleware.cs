using Alissa.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alissa.Core.Api
{
    /// <summary>
    /// Middleware for API authentication and validation.
    /// Enforces allowed hosts and shared secret header.
    /// </summary>
    public class ApiAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiConfig _apiConfig;
        private readonly HashSet<string> _allowedHosts;

        public ApiAuthenticationMiddleware(RequestDelegate next, ApiConfig apiConfig)
        {
            _next = next;
            _apiConfig = apiConfig;
            _allowedHosts = new HashSet<string>(_apiConfig.AllowedHosts);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isApiPath = context.Request.Path.StartsWithSegments("/api");

            if (!isApiPath)
            {
                await _next(context);
                return;
            }

            bool hostCheckPassed = ValidateHostAccess(context);

            if (!hostCheckPassed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Access denied from this host" });
                return;
            }

            bool secretCheckPassed = ValidateSharedSecret(context);

            if (!secretCheckPassed)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing shared secret" });
                return;
            }

            bool sizeCheckPassed = ValidateRequestSize(context);

            if (!sizeCheckPassed)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new { error = "Request body too large" });
                return;
            }

            await _next(context);
        }

        private bool ValidateHostAccess(HttpContext context)
        {
            string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            bool isAllowed = _allowedHosts.Contains(remoteIp);

            return isAllowed;
        }

        private bool ValidateSharedSecret(HttpContext context)
        {
            bool requiresSecret = !string.IsNullOrWhiteSpace(_apiConfig.SharedSecretHeader);

            if (!requiresSecret)
            {
                return true;
            }

            bool hasSecretHeader = context.Request.Headers.TryGetValue("X-Alissa-Secret", out var secretValue);
            bool secretMatches = hasSecretHeader && secretValue == _apiConfig.SharedSecretHeader;

            return secretMatches;
        }

        private bool ValidateRequestSize(HttpContext context)
        {
            long? contentLength = context.Request.ContentLength;
            bool exceedsLimit = contentLength.HasValue && contentLength.Value > _apiConfig.MaxRequestBodyBytes;

            return !exceedsLimit;
        }
    }

    /// <summary>
    /// Extension methods for registering API middleware.
    /// </summary>
    public static class ApiMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiAuthentication(
            this IApplicationBuilder builder,
            ApiConfig apiConfig)
        {
            return builder.UseMiddleware<ApiAuthenticationMiddleware>(apiConfig);
        }
    }
}

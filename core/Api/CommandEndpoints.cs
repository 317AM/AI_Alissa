using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alissa.Core.Api
{
    /// <summary>
    /// Command API endpoints.
    /// Routes POST /api/command/{name} requests through CommandService.
    /// </summary>
    public static class CommandEndpoints
    {
        public static void MapCommandEndpoints(
            this WebApplication app,
            ICommandService commandService)
        {
            app.MapPost("/api/command/{name}", HandleCommand);

            async Task<IResult> HandleCommand(
                HttpContext context,
                string name,
                HttpRequest request)
            {
                var result = await ProcessCommandAsync(name, request, commandService);
                return result;
            }
        }

        private static async Task<IResult> ProcessCommandAsync(
            string name,
            HttpRequest request,
            ICommandService commandService)
        {
            try
            {
                var parameters = await ParseRequestParametersAsync(request);
                string result = await commandService.ExecuteCommandAsync(name, parameters);

                var response = new CommandResponse { Result = result };
                return Results.Ok(response);
            }
            catch (System.Exception ex)
            {
                var errorResponse = new CommandResponse
                {
                    Result = $"Error executing command: {ex.Message}"
                };

                return Results.BadRequest(errorResponse);
            }
        }

        private static async Task<Dictionary<string, object>> ParseRequestParametersAsync(HttpRequest request)
        {
            var parameters = new Dictionary<string, object>();

            using (var reader = new StreamReader(request.Body))
            {
                string bodyText = await reader.ReadToEndAsync();
                bool hasBody = !string.IsNullOrWhiteSpace(bodyText);

                if (!hasBody)
                {
                    return parameters;
                }

                bool parsed = TryParseJsonObject(bodyText, parameters);

                if (!parsed)
                {
                    parameters["arg"] = bodyText;
                }
            }

            return parameters;
        }

        private static bool TryParseJsonObject(string bodyText, Dictionary<string, object> parameters)
        {
            try
            {
                var jsonObject = JsonSerializer.Deserialize<JsonElement>(bodyText);
                bool isObject = jsonObject.ValueKind == JsonValueKind.Object;

                if (!isObject)
                {
                    return false;
                }

                foreach (var property in jsonObject.EnumerateObject())
                {
                    parameters[property.Name] = property.Value.ToString();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Response model for command execution.
    /// </summary>
    public class CommandResponse
    {
        /// <summary>
        /// Result of command execution (as string).
        /// </summary>
        public string Result { get; set; } = string.Empty;
    }
}

using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alissa.Core.Api
{
    /// <summary>
    /// Code integration API endpoints.
    /// Maps POST /api/code/* requests to CodeIntegrationService and PersonaService.
    /// </summary>
    public static class CodeEndpoints
    {
        public static void MapCodeEndpoints(
            this WebApplication app,
            ICodeIntegrationService codeIntegrationService,
            string basePath)
        {
            app.MapPost("/api/code/context", HandleCodeContext);
            app.MapPost("/api/code/ask", HandleCodeAsk);
            app.MapPost("/api/code/apply", HandleCodeApply);
            app.MapGet("/api/code/status", HandleCodeStatus);

            async Task<IResult> HandleCodeContext(HttpContext context, CodeContextMessage request)
            {
                var result = await ProcessCodeContextAsync(request, basePath);
                return result;
            }

            async Task<IResult> HandleCodeAsk(HttpContext context, CodeContextMessage request)
            {
                var result = await ProcessCodeAskAsync(request, basePath, codeIntegrationService);
                return result;
            }

            async Task<IResult> HandleCodeApply(HttpContext context, ApplyCodeRequest request)
            {
                var result = await ProcessCodeApplyAsync(request, codeIntegrationService);
                return result;
            }

            async Task<IResult> HandleCodeStatus(HttpContext context)
            {
                var result = await ProcessCodeStatusAsync(basePath);
                return result;
            }
        }

        private static async Task<IResult> ProcessCodeContextAsync(CodeContextMessage request, string basePath)
        {
            try
            {
                string language = NormalizeLanguage(request.Language);
                string task = NormalizeTask(request.Task);

                await PersonaService.UpdateCurrentCodeAsync(basePath, request.FilePath, language, task);

                var response = new CodeContextResponse
                {
                    Success = true,
                    Message = "Code context updated successfully"
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CodeContextResponse
                {
                    Success = false,
                    Message = $"Error updating context: {ex.Message}"
                };

                return Results.BadRequest(errorResponse);
            }
        }

        private static async Task<IResult> ProcessCodeAskAsync(
            CodeContextMessage request,
            string basePath,
            ICodeIntegrationService codeIntegrationService)
        {
            try
            {
                string language = NormalizeLanguage(request.Language);
                string task = NormalizeTask(request.Task);

                await PersonaService.UpdateCurrentCodeAsync(basePath, request.FilePath, language, task);

                bool isAnalysisTask = task == "explain" || task == "review";
                var response = isAnalysisTask
                    ? await HandleAnalysisTaskAsync(request, language, codeIntegrationService)
                    : await HandleGenerationTaskAsync(request, language, task, codeIntegrationService);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CodeContextResponse
                {
                    Success = false,
                    Message = $"Error processing task: {ex.Message}"
                };

                return Results.BadRequest(errorResponse);
            }
        }

        private static async Task<CodeContextResponse> HandleAnalysisTaskAsync(
            CodeContextMessage request,
            string language,
            ICodeIntegrationService codeIntegrationService)
        {
            string codeContent = string.IsNullOrWhiteSpace(request.SelectedText)
                ? request.FullFileContent
                : request.SelectedText;

            string analysis = await codeIntegrationService.AnalyzeCodeAsync(codeContent, language);

            var response = new CodeContextResponse
            {
                Success = true,
                Message = $"Analysis completed",
                Notes = new List<string> { analysis }
            };

            bool hasError = !string.IsNullOrEmpty(request.ErrorMessage);

            if (hasError)
            {
                response.Notes.Add($"Error context: {request.ErrorMessage}");
            }

            return response;
        }

        private static async Task<CodeContextResponse> HandleGenerationTaskAsync(
            CodeContextMessage request,
            string language,
            string task,
            ICodeIntegrationService codeIntegrationService)
        {
            string codeContent = string.IsNullOrWhiteSpace(request.SelectedText)
                ? request.FullFileContent
                : request.SelectedText;

            string requirement = BuildRequirementString(task, request.ErrorMessage);
            string suggestedCode = await codeIntegrationService.GenerateModificationAsync(codeContent, requirement, language);

            var response = new CodeContextResponse
            {
                Success = true,
                Message = $"Generated suggestion for {task}",
                SuggestedCode = suggestedCode
            };

            return response;
        }

        private static async Task<IResult> ProcessCodeApplyAsync(
            ApplyCodeRequest request,
            ICodeIntegrationService codeIntegrationService)
        {
            try
            {
                bool hasValidInput = ValidateApplyRequest(request, out string validationError);

                if (!hasValidInput)
                {
                    return Results.BadRequest(new CodeContextResponse
                    {
                        Success = false,
                        Message = validationError
                    });
                }

                bool isProtected = codeIntegrationService.IsPathProtected(request.FilePath);

                if (isProtected)
                {
                    var protectedResponse = new CodeContextResponse
                    {
                        Success = false,
                        Message = $"Cannot modify protected directory: {request.FilePath}"
                    };

                    return Results.Json(protectedResponse, statusCode: StatusCodes.Status409Conflict);
                }

                bool isSafe = await codeIntegrationService.IsFileSafeToModifyAsync(request.FilePath);

                if (!isSafe)
                {
                    var unsafeResponse = new CodeContextResponse
                    {
                        Success = false,
                        Message = "File is not safe to modify (may be locked or protected)"
                    };

                    return Results.Json(unsafeResponse, statusCode: StatusCodes.Status409Conflict);
                }

                string backupPath = await codeIntegrationService.CreateBackupAsync(request.FilePath);
                await codeIntegrationService.WriteModificationAsync(request.FilePath, request.ModifiedCode);

                var successResponse = new CodeContextResponse
                {
                    Success = true,
                    Message = "Code modification applied successfully",
                    BackupPath = backupPath
                };

                return Results.Ok(successResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = new CodeContextResponse
                {
                    Success = false,
                    Message = $"Error applying modification: {ex.Message}"
                };

                return Results.BadRequest(errorResponse);
            }
        }

        private static async Task<IResult> ProcessCodeStatusAsync(string basePath)
        {
            try
            {
                var currentCode = await PersonaService.GetCurrentCodeAsync(basePath);

                if (currentCode == null)
                {
                    return Results.Ok(new CodeContextResponse
                    {
                        Success = true,
                        Message = "No current code context"
                    });
                }

                var response = new CodeContextResponse
                {
                    Success = true,
                    Message = "Current code context retrieved",
                    Notes = new List<string>
                    {
                        $"File: {currentCode.Name}",
                        $"Language: {currentCode.Language}",
                        $"Task: {currentCode.Task}"
                    }
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new CodeContextResponse
                {
                    Success = false,
                    Message = $"Error retrieving status: {ex.Message}"
                };

                return Results.BadRequest(errorResponse);
            }
        }

        private static string NormalizeLanguage(string language)
        {
            return string.IsNullOrWhiteSpace(language) ? "unknown" : language;
        }

        private static string NormalizeTask(string task)
        {
            return string.IsNullOrWhiteSpace(task) ? "explain" : task;
        }

        private static string BuildRequirementString(string task, string errorMessage)
        {
            return task switch
            {
                "fix" => $"Fix the code. Error: {errorMessage}",
                "test" => "Write tests for this code",
                _ => $"Complete or refactor this code for: {task}"
            };
        }

        private static bool ValidateApplyRequest(ApplyCodeRequest request, out string error)
        {
            bool hasValidPath = !string.IsNullOrWhiteSpace(request.FilePath);
            bool hasValidCode = !string.IsNullOrWhiteSpace(request.ModifiedCode);

            bool isValid = hasValidPath && hasValidCode;
            error = isValid ? string.Empty : "Missing required fields: filePath and modifiedCode";

            return isValid;
        }
    }

    /// <summary>
    /// Request model for applying code modifications.
    /// </summary>
    public class ApplyCodeRequest
    {
        /// <summary>
        /// Full path to the file to modify.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// The modified code to write to the file.
        /// </summary>
        public string ModifiedCode { get; set; } = string.Empty;
    }
}

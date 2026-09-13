using System.Net;
using System.Text.Json;
using Alissa.Core.Api;
using Alissa.Core.Interfaces;
using Alissa.Core.Models;
using Alissa.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Alissa.Tests
{
    /// <summary>
    /// Integration tests for Alissa HTTP API endpoints.
    /// Tests code context, code ask, code apply, and command routing endpoints.
    /// Note: These tests are currently skipped as they require a full ASP.NET Core host setup.
    /// </summary>
    public class ApiIntegrationTests : IDisposable
    {
        private readonly string _testBasePath;
        private readonly HttpClient _client;

        public ApiIntegrationTests()
        {
            // Create a temporary directory for test files
            _testBasePath = Path.Combine(Path.GetTempPath(), $"alissa_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testBasePath);
            Directory.CreateDirectory(Path.Combine(_testBasePath, "config"));
            Directory.CreateDirectory(Path.Combine(_testBasePath, "backups"));

            // For now, use a mock client
            _client = new HttpClient();
        }

        public void Dispose()
        {
            _client?.Dispose();

            // Cleanup temp directory
            try
            {
                Directory.Delete(_testBasePath, true);
            }
            catch { }
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCodeContext_UpdatesPersonaSuccessfully()
        {
            // Arrange
            var request = new CodeContextMessage
            {
                FilePath = "/path/to/test.cs",
                Language = "csharp",
                SelectedText = "public void Test() { }",
                FullFileContent = "public class Test { public void Test() { } }",
                Task = ""
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/context", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeContextResponse>(responseBody);

            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.Contains("updated", result.Message);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCodeAsk_ExplainTask_ReturnsNotes()
        {
            // Arrange
            var request = new CodeContextMessage
            {
                FilePath = "/path/to/test.cs",
                Language = "csharp",
                SelectedText = "var x = 5;",
                FullFileContent = "var x = 5;",
                Task = "explain"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/ask", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeContextResponse>(responseBody);

            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.NotEmpty(result.Notes);
            Assert.Null(result.SuggestedCode);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCodeAsk_FixTask_ReturnsSuggestedCode()
        {
            // Arrange
            var request = new CodeContextMessage
            {
                FilePath = "/path/to/test.cs",
                Language = "csharp",
                SelectedText = "var x = undefinedVariable;",
                FullFileContent = "var x = undefinedVariable;",
                Task = "fix",
                ErrorMessage = "error CS0103: The name 'undefinedVariable' does not exist"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/ask", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeContextResponse>(responseBody);

            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.NotEmpty(result.SuggestedCode);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCodeApply_ValidFile_CreatesBackupAndWritesFile()
        {
            // Arrange
            var testFilePath = Path.Combine(_testBasePath, "test.cs");
            var originalContent = "public class Test { }";
            await File.WriteAllTextAsync(testFilePath, originalContent);

            var applyRequest = new { FilePath = testFilePath, ModifiedCode = "public class Test { public void Modified() { } }" };
            var json = JsonSerializer.Serialize(applyRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/apply", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeContextResponse>(responseBody);

            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.NotEmpty(result.BackupPath);

            // Verify backup exists
            var backupPath = Path.Combine(_testBasePath, result.BackupPath!);
            Assert.True(File.Exists(backupPath));

            // Verify file was modified
            var updatedContent = await File.ReadAllTextAsync(testFilePath);
            Assert.Contains("Modified", updatedContent);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCodeApply_ProtectedDirectory_Returns409Conflict()
        {
            // Arrange
            var protectedPath = Path.Combine(_testBasePath, "config", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(protectedPath)!);
            await File.WriteAllTextAsync(protectedPath, "{}");

            var applyRequest = new { FilePath = protectedPath, ModifiedCode = "{}" };
            var json = JsonSerializer.Serialize(applyRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/apply", content);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeContextResponse>(responseBody);

            Assert.NotNull(result);
            Assert.False(result!.Success);
            Assert.Contains("protected", result.Message);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCodeApply_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange
            var applyRequest = new { FilePath = "" }; // Missing modifiedCode
            var json = JsonSerializer.Serialize(applyRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/apply", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task GetCodeStatus_ReturnsCurrentContext()
        {
            // Arrange
            // First, set some context
            var contextRequest = new CodeContextMessage
            {
                FilePath = "/path/to/test.cs",
                Language = "csharp",
                Task = "explain"
            };

            var json = JsonSerializer.Serialize(contextRequest);
            var setContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/code/context", setContent);

            // Act
            var response = await _client.GetAsync("/api/code/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeContextResponse>(responseBody);

            Assert.NotNull(result);
            Assert.True(result!.Success);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task PostCommand_ExecuteRegisteredCommand_ReturnsResult()
        {
            // Arrange
            // Assuming /read command is registered
            var commandRequest = new { arg = "/path/to/file.cs" };
            var json = JsonSerializer.Serialize(commandRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/command/read", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CommandResponse>(responseBody);

            Assert.NotNull(result);
            Assert.NotEmpty(result!.Result);
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task Health_Check_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

            Assert.Equal("healthy", result.GetProperty("status").GetString());
        }

        [Fact(Skip = "API tests require full host setup")]
        public async Task RequestBodySize_ExceedsLimit_Returns413PayloadTooLarge()
        {
            // Arrange
            var largeContent = new string('x', 6_000_000); // Exceeds default 5MB limit
            var request = new { FilePath = "/path/to/test.cs", ModifiedCode = largeContent };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/apply", content);

            // Assert
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }
    }

    /// <summary>
    /// Model for deserialized command response.
    /// </summary>
    public class CommandResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test fixture helpers
    /// </summary>
    public static class TestHelpers
    {
        public static async Task<T?> DeserializeAsync<T>(HttpContent content)
        {
            var json = await content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}


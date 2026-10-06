using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Services;

namespace TalentFlow.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;
    private readonly string _agenticAiUrl;

    public ChatService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _agenticAiUrl = (Environment.GetEnvironmentVariable("AI_SERVICE_URL")
            ?? configuration["AIService:BaseUrl"]
            ?? "http://localhost:8000").TrimEnd('/');
    }

    public async Task<Result<ChatResponseDto>> ProcessChatAsync(ChatRequestDto request, string authToken, CancellationToken cancellationToken)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_agenticAiUrl}/api/chat");

            if (!string.IsNullOrEmpty(authToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                messages = request.Messages,
                company_id = request.CompanyId.ToString()
            }, jsonOptions), Encoding.UTF8, "application/json");

            requestMessage.Content = content;

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Chat API failed with status {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                return Result<ChatResponseDto>.Failure("Failed to communicate with AI Chat Service.");
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponseDto>(cancellationToken: cancellationToken);
            if (result == null)
            {
                return Result<ChatResponseDto>.Failure("Invalid response from AI Chat Service.");
            }

            return Result<ChatResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing chat request");
            return Result<ChatResponseDto>.Failure("An unexpected error occurred processing the chat request.");
        }
    }
}

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TalentFlow.Infrastructure.AI
{
    public interface IAgentServiceClient
    {
        Task<bool> StartScreeningWorkflowAsync(string workflowId, string applicationId, string jobId, string initiatedBy, string companyId, string? authToken = null);
    }

    public class AgentServiceClient : IAgentServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentServiceClient> _logger;

        public AgentServiceClient(HttpClient httpClient, ILogger<AgentServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // The base address should be configured in DI, typically http://localhost:8000
        }

        public async Task<bool> StartScreeningWorkflowAsync(string workflowId, string applicationId, string jobId, string initiatedBy, string companyId, string? authToken = null)
        {
            try
            {
                _logger.LogInformation("Sending start request to AI service for Workflow {WorkflowId}", workflowId);
                
                var request = new
                {
                    workflow_id = workflowId,
                    application_id = applicationId,
                    job_id = jobId,
                    initiated_by = initiatedBy,
                    company_id = companyId
                };

                using var message = new HttpRequestMessage(HttpMethod.Post, "api/screening/start")
                {
                    Content = JsonContent.Create(request)
                };
                if (!string.IsNullOrWhiteSpace(authToken))
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                var response = await _httpClient.SendAsync(message);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AI service successfully started Workflow {WorkflowId}", workflowId);
                    return true;
                }

                _logger.LogWarning("AI service returned status {StatusCode} for Workflow {WorkflowId}", response.StatusCode, workflowId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to communicate with AI service for Workflow {WorkflowId}", workflowId);
                return false;
            }
        }
    }
}

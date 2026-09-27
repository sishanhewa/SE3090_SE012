using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TalentFlow.Infrastructure.AI
{
    public interface IAgentServiceClient
    {
        Task<bool> StartScreeningWorkflowAsync(string workflowId, string applicationId, string jobId, string initiatedBy, string companyId);
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

        public async Task<bool> StartScreeningWorkflowAsync(string workflowId, string applicationId, string jobId, string initiatedBy, string companyId)
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

                var response = await _httpClient.PostAsJsonAsync("/api/workflows/screening/start", request);
                
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

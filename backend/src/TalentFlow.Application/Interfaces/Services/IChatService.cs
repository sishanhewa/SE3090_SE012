using System;
using System.Threading;
using System.Threading.Tasks;
using TalentFlow.Application.Common;
using System.Collections.Generic;

namespace TalentFlow.Application.Interfaces.Services;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatRequestDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public Guid CompanyId { get; set; }
}

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
}

public interface IChatService
{
    Task<Result<ChatResponseDto>> ProcessChatAsync(ChatRequestDto request, string authToken, CancellationToken cancellationToken);
}

using System;
using System.Text.Json.Serialization;

namespace ChatBotApi.Application.Common.DTOs;

public class ChatMessageDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

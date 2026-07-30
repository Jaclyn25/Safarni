namespace ChatBotApi.Application.Common.Models;

public sealed record AICompletionOptions(
    double? Temperature = null,
    int? MaxTokens = null,
    double? TopP = null);

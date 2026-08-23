using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Community;
using Safarni.Application.Features.Community.Commands;
using Safarni.Application.Features.Community.Queries;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/community")]
public sealed class CommunityController : ControllerBase
{
    private const long MaxCommunityMediaRequestSizeBytes = 60 * 1024 * 1024;

    private readonly ISender _sender;

    public CommunityController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed(
        [FromQuery] CommunityPostType? type,
        [FromQuery] string? destination,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetCommunityFeedQuery(type, destination, take), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyCollection<CommunityPostResponse>>(result.Error, "Failed to load the community feed.")
            : Ok(ApiResponse<IReadOnlyCollection<CommunityPostResponse>>.SuccessResponse(result.Value!, "Community feed loaded successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("posts/{postId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(Guid postId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCommunityPostDetailsQuery(postId), cancellationToken);
        return result.IsFailure
            ? Failure<CommunityPostResponse>(result.Error, "Failed to load the community post.")
            : Ok(ApiResponse<CommunityPostResponse>.SuccessResponse(result.Value!, "Community post loaded successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedPosts([FromQuery] int take = 10, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSavedCommunityPostsQuery(take), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyCollection<CommunityPostResponse>>(result.Error, "Failed to load saved posts.")
            : Ok(ApiResponse<IReadOnlyCollection<CommunityPostResponse>>.SuccessResponse(result.Value!, "Saved posts loaded successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("trending-destinations")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrendingDestinations([FromQuery] int take = 5, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTrendingCommunityDestinationsQuery(take), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyCollection<CommunityDestinationTrendResponse>>(result.Error, "Failed to load trending destinations.")
            : Ok(ApiResponse<IReadOnlyCollection<CommunityDestinationTrendResponse>>.SuccessResponse(result.Value!, "Trending destinations loaded successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreateCommunityPostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCommunityPostCommand(request.Type, request.Title, request.Body, request.Destination, request.LinkedTripId, request.Media),
            cancellationToken);

        return result.IsFailure
            ? Failure<CommunityPostResponse>(result.Error, "Failed to create the community post.")
            : Ok(ApiResponse<CommunityPostResponse>.SuccessResponse(result.Value!, "Community post created successfully.", StatusCodes.Status200OK));
    }

    [HttpPut("posts/{postId:guid}")]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromBody] UpdateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCommunityPostCommand(postId, request.Type, request.Title, request.Body, request.Destination, request.LinkedTripId, request.Media),
            cancellationToken);
        return result.IsFailure
            ? Failure<CommunityPostResponse>(result.Error, "Failed to update the community post.")
            : Ok(ApiResponse<CommunityPostResponse>.SuccessResponse(result.Value!, "Community post updated successfully.", StatusCodes.Status200OK));
    }

    [HttpDelete("posts/{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCommunityPostCommand(postId), cancellationToken);
        return result.IsFailure
            ? Failure<Guid>(result.Error, "Failed to delete the community post.")
            : Ok(ApiResponse<Guid>.SuccessResponse(result.Value, "Community post deleted successfully.", StatusCodes.Status200OK));
    }

    [HttpPut("posts/{postId:guid}")]
    public async Task<IActionResult> UpdatePost(
        Guid postId,
        [FromBody] UpdateCommunityPostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateCommunityPostCommand(postId, request.Title, request.Body, request.Destination, request.Media),
            cancellationToken);

        return result.IsFailure
            ? Failure<CommunityPostResponse>(result.Error, "Failed to update the community post.")
            : Ok(ApiResponse<CommunityPostResponse>.SuccessResponse(result.Value!, "Community post updated successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("media")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxCommunityMediaRequestSizeBytes)]
    public async Task<IActionResult> UploadMedia(
        [FromForm] UploadCommunityMediaRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return Failure<UploadCommunityMediaResponse>(
                new Error("Community.InvalidMedia", "A media file is required.", ErrorType.Validation),
                "Media upload failed.");
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var result = await _sender.Send(
            new UploadCommunityMediaCommand(
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                memoryStream.ToArray()),
            cancellationToken);

        return result.IsFailure
            ? Failure<UploadCommunityMediaResponse>(result.Error, "Media upload failed.")
            : Ok(ApiResponse<UploadCommunityMediaResponse>.SuccessResponse(result.Value!, "Media uploaded successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("posts/{postId:guid}/like")]
    public Task<IActionResult> ToggleLike(Guid postId, CancellationToken cancellationToken) =>
        ToggleInteraction(postId, CommunityPostInteractionType.Like, cancellationToken);

    [HttpPost("posts/{postId:guid}/save")]
    public Task<IActionResult> ToggleSave(Guid postId, CancellationToken cancellationToken) =>
        ToggleInteraction(postId, CommunityPostInteractionType.Save, cancellationToken);

    [HttpPost("posts/{postId:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid postId,
        [FromBody] CreateCommunityCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCommunityCommentCommand(postId, request.Body, request.ParentCommentId, request.Media),
            cancellationToken);

        return result.IsFailure
            ? Failure<CreateCommunityCommentResult>(result.Error, "Failed to add the comment.")
            : Ok(ApiResponse<CreateCommunityCommentResult>.SuccessResponse(result.Value!, "Comment added successfully.", StatusCodes.Status200OK));
    }

    private async Task<IActionResult> ToggleInteraction(
        Guid postId,
        CommunityPostInteractionType type,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ToggleCommunityPostInteractionCommand(postId, type), cancellationToken);
        return result.IsFailure
            ? Failure<ToggleCommunityPostInteractionResult>(result.Error, "Failed to update the post interaction.")
            : Ok(ApiResponse<ToggleCommunityPostInteractionResult>.SuccessResponse(result.Value!, "Post interaction updated successfully.", StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, ApiResponse<T>.FailureResponse(
            new[] { new ApiError(error.Code, error.Message) }, message, statusCode));
    }
}

public sealed record CreateCommunityPostRequest(
    CommunityPostType Type,
    string Title,
    string Body,
    string? Destination,
    Guid? LinkedTripId,
    IReadOnlyCollection<CommunityMediaInput>? Media);

public sealed record CreateCommunityCommentRequest(string Body, Guid? ParentCommentId = null);
public sealed record UploadCommunityMediaRequest(IFormFile File);

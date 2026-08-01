using FluentValidation;
using ChatBotApi.Application.Features.Chat.StreamChatResponse;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Behaviors;
using Safarni.Application.Features.Auth.ChangePassword;
using Safarni.Application.Features.Auth.ForgotPassword;
using Safarni.Application.Features.Auth.GoogleLogin;
using Safarni.Application.Features.Auth.Login;
using Safarni.Application.Features.Auth.Logout;
using Safarni.Application.Features.Auth.RefreshTokens;
using Safarni.Application.Features.Auth.Register;
using Safarni.Application.Features.Auth.ResetPassword;
using Safarni.Application.Features.Auth.VerifyEmail;
using Safarni.Application.Features.Preferences.UpsertTravelProfile;
using Safarni.Application.Features.Profile.UpdateMyProfile;
using Safarni.Application.Features.Profile.UploadMyProfile;
using Safarni.Application.Features.Profile.UploadProfilePhoto;
using Safarni.Application.Features.Settings.UpdateMySettings;
using Safarni.Application.Features.Trips.CreateTrip;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Application.Features.Trips.GenerateItinerary.Jobs;
using Safarni.Application.Features.Trips.UpdateTrip;
using Safarni.Application.Features.Trips.Itineraries.ManageItems;
using Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;
using Safarni.Application.Features.VisaApplications.CreateVisaApplication;
using Safarni.Application.Features.VisaApplications.SelectVisaType;

namespace Safarni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddTransient<IValidator<RegisterUserCommand>, RegisterUserCommandValidator>();
        services.AddTransient<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddTransient<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddTransient<IValidator<LogoutCommand>, LogoutCommandValidator>();
        services.AddTransient<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddTransient<IValidator<VerifyEmailCommand>, VerifyEmailCommandValidator>();
        services.AddTransient<IValidator<ChangePasswordCommand>, ChangePasswordCommandValidator>();
        services.AddTransient<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();
        services.AddTransient<IValidator<GoogleLoginCommand>, GoogleLoginCommandValidator>();

        services.AddTransient<IValidator<UpdateMyProfileCommand>, UpdateMyProfileCommandValidator>();
        services.AddTransient<IValidator<UploadProfilePhotoCommand>, UploadProfilePhotoCommandValidator>();
        services.AddTransient<IValidator<UpdateMySettingsCommand>, UpdateMySettingsCommandValidator>();

        services.AddTransient<IValidator<UpsertTravelProfileCommand>, UpsertTravelProfileCommandValidator>();
        services.AddTransient<IValidator<CreateVisaApplicationCommand>, CreateVisaApplicationCommandValidator>();
        services.AddTransient<IValidator<SelectVisaTypeCommand>, SelectVisaTypeCommandValidator>();

        services.AddTransient<IValidator<CreateTripCommand>, CreateTripCommandValidator>();
        services.AddTransient<IValidator<UpdateTripCommand>, UpdateTripCommandValidator>();
        services.AddTransient<IValidator<GenerateItineraryCommand>, GenerateItineraryCommandValidator>();
        services.AddTransient<
            IValidator<StartItineraryGenerationCommand>,
            StartItineraryGenerationCommandValidator>();
        services.AddScoped<
            IItineraryGenerationProcessor,
            ItineraryGenerationProcessor>();
        services.AddScoped<ItineraryVenueSuggestionLoader>();
        services.AddTransient<IValidator<AddItineraryItemCommand>, AddItineraryItemCommandValidator>();
        services.AddTransient<IValidator<UpdateItineraryItemCommand>, UpdateItineraryItemCommandValidator>();
        services.AddTransient<
            IValidator<MoveItineraryItemCommand>,
            MoveItineraryItemCommandValidator>();
        services.AddTransient<
            IValidator<SelectItineraryVenueCommand>,
            SelectItineraryVenueCommandValidator>();

        // Chatbot validators
        services.AddTransient<IValidator<ChatBotApi.Application.Features.Chat.Commands.CreateConversation.CreateConversationCommand>, ChatBotApi.Application.Features.Chat.Commands.CreateConversation.CreateConversationCommandValidator>();
        services.AddTransient<IValidator<ChatBotApi.Application.Features.Chat.Commands.SendMessage.SendMessageCommand>, ChatBotApi.Application.Features.Chat.Commands.SendMessage.SendMessageCommandValidator>();
        services.AddTransient<
            IValidator<StreamChatResponseCommand>,
            StreamChatResponseCommandValidator>();

        return services;
    }
}

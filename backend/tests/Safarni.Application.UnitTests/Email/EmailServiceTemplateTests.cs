using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Safarni.Infrastructure.Email;
using Xunit;

namespace Safarni.Application.UnitTests.Email;

public class EmailServiceTemplateTests
{
    private const string Recipient = "traveler@example.com";

    [Fact]
    public void BuildEmailShell_ContainsBrand_Heading_AndFooter()
    {
        var html = EmailService.BuildEmailShell("Verify Email", "<p>body</p>");

        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("SAFARNI");
        html.Should().Contain("<h1");
        html.Should().Contain(">Verify Email</h1>");
        html.Should().Contain("<p>body</p>");
        html.Should().Contain("You received this email because of your activity on Safarni.");
        html.Should().Contain($"background:{EmailService.BodyBackgroundColor}");
    }

    [Fact]
    public void BuildEmailShell_EncodesHeading()
    {
        var html = EmailService.BuildEmailShell("<script>alert('x')</script>", string.Empty);

        html.Should().NotContain("<script>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Button_EmbedsLinkAndLabel_WithAccentStyle()
    {
        var html = EmailService.Button("https://localhost/verify?token=abc", "Verify Email");

        html.Should().Contain("href=\"https://localhost/verify?token=abc\"");
        html.Should().Contain(">Verify Email</a>");
        html.Should().Contain($"background:{EmailService.AccentColor}");
        html.Should().Contain("display:inline-block");
    }

    [Fact]
    public void Button_EncodesLabel()
    {
        var html = EmailService.Button("https://localhost/x", "Reset & Sign <in>");

        html.Should().Contain("Reset &amp; Sign &lt;in&gt;</a>");
    }

    [Fact]
    public void FallbackLink_EmbedsRawLink_AndActionLabel()
    {
        var html = EmailService.FallbackLink("https://localhost/planner/abc", "open your itinerary");

        html.Should().Contain("copy and paste this link into your browser to open your itinerary:");
        html.Should().Contain("https://localhost/planner/abc");
        html.Should().Contain($"color:{EmailService.MutedTextColor}");
    }

    [Fact]
    public void DetailsBox_RendersAllRows_EncodesValues()
    {
        var html = EmailService.DetailsBox(
            ("Destination", "Cairo <Egypt>"),
            ("Duration", "3 days"));

        html.Should().Contain(">Destination</td>");
        html.Should().Contain("Cairo &lt;Egypt&gt;</td>");
        html.Should().Contain(">3 days</td>");
        html.Should().Contain($"background:{EmailService.BodyBackgroundColor}");
    }

    [Fact]
    public void BuildHighlightsSection_EmptySummary_ReturnsFallbackParagraph()
    {
        var html = EmailService.BuildHighlightsSection("   ");

        html.Should().Contain("Planner to explore the full day-by-day plan.");
        html.Should().NotContain("<ul>");
    }

    [Fact]
    public void BuildHighlightsSection_WithSummary_WrapsIt()
    {
        var html = EmailService.BuildHighlightsSection(
            "<ul><li>Day 1: Pyramids</li></ul>");

        html.Should().Contain("Here are a few highlights from your plan:");
        html.Should().Contain("<ul><li>Day 1: Pyramids</li></ul>");
    }

    [Fact]
    public async Task SendEmailVerification_DevelopmentUnconfigured_LogsLinkAndCompletes()
    {
        var (service, logger) = CreateService();

        await service.SendEmailVerificationAsync(
            Recipient,
            "https://localhost/verify-email?token=t1");

        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Development link:") &&
            entry.Message.Contains("https://localhost/verify-email?token=t1"));
    }

    [Fact]
    public async Task SendEmailVerification_ProductionUnconfigured_Throws()
    {
        var (service, _) = CreateService(isDevelopment: false);

        var act = async () => await service.SendEmailVerificationAsync(
            Recipient,
            "https://localhost/verify-email");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("EmailSettings are not configured*");
    }

    [Fact]
    public async Task SendItinerarySummary_DevelopmentUnconfigured_LogsPlannerLinkAndCompletes()
    {
        var (service, logger) = CreateService();

        const string itineraryLink = "https://localhost/planner/trip-123";

        await service.SendItinerarySummaryAsync(
            Recipient,
            "Sama Name",
            "Cairo Trip",
            "Cairo",
            new DateTime(2026, 9, 10),
            new DateTime(2026, 9, 12),
            "<ul><li>Day 1: Pyramids</li></ul>",
            itineraryLink);

        logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Development link:") &&
            entry.Message.Contains(itineraryLink));
    }

    private static (EmailService Service, MockLogger<EmailService> Logger) CreateService(
        bool isDevelopment = true)
    {
        var settings = new EmailSettings
        {
            DisplayName = "Safarni",
            From = "YOUR_FROM_ADDRESS",
            Host = "YOUR_SMTP_HOST",
            Port = 587,
            UserName = "YOUR_SMTP_USER",
            Password = "YOUR_SMTP_PASSWORD",
            UseStartTls = true,
        };

        var environment = new Mock<IHostEnvironment>();
        environment
            .Setup(host => host.EnvironmentName)
            .Returns(isDevelopment ? Environments.Development : Environments.Production);

        var logger = new MockLogger<EmailService>();

        var service = new EmailService(
            Options.Create(settings),
            environment.Object,
            logger);

        return (service, logger);
    }

    private sealed class MockLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => new NoOpScope();

        private sealed class NoOpScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
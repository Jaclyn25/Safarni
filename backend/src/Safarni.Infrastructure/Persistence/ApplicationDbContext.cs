using ChatBotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Safarni tables
    public DbSet<User> Users => Set<User>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripCity> TripCities => Set<TripCity>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<ItineraryGenerationJob> ItineraryGenerationJobs =>
        Set<ItineraryGenerationJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TravelPreference> TravelPreferences => Set<TravelPreference>();
    public DbSet<PassportInfo> PassportInfos => Set<PassportInfo>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AIRequestLog> AIRequestLogs => Set<AIRequestLog>();
    public DbSet<TravelAgency> TravelAgencies => Set<TravelAgency>();
    public DbSet<AgencyVerificationDocument> AgencyVerificationDocuments =>
        Set<AgencyVerificationDocument>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<TravelPackage> TravelPackages => Set<TravelPackage>();
    public DbSet<TravelPackageDay> TravelPackageDays => Set<TravelPackageDay>();
    public DbSet<PackageBookingRequest> PackageBookingRequests => Set<PackageBookingRequest>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // Chatbot tables
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<TravelKnowledge> TravelKnowledges => Set<TravelKnowledge>();
    public DbSet<ChatFolder> ChatFolders => Set<ChatFolder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Folder)
                .WithMany(f => f.Conversations)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

        });



        modelBuilder.Entity<ChatFolder>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<TravelKnowledge>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.SourceUrl)
                .HasMaxLength(500);

            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Ignore(e => e.Embedding);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.TargetType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.TargetId)
                .HasMaxLength(100);

            entity.Property(e => e.MetadataJson)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(64);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.Severity)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.ActorUserId);
        });

        modelBuilder.Entity<AIRequestLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Provider)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Model)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Operation)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.ErrorCode)
                .HasMaxLength(100);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.UserId);
        });
    }
}

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

    // Travel Agency / Packages
    public DbSet<TravelAgency> TravelAgencies => Set<TravelAgency>();
    public DbSet<AgencyPayoutProfile> AgencyPayoutProfiles => Set<AgencyPayoutProfile>();

    public DbSet<AgencyVerificationDocument> AgencyVerificationDocuments =>
        Set<AgencyVerificationDocument>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    public DbSet<TravelPackage> TravelPackages => Set<TravelPackage>();
    public DbSet<TravelPackageDay> TravelPackageDays => Set<TravelPackageDay>();
    public DbSet<TravelPackageCity> TravelPackageCities => Set<TravelPackageCity>();
    public DbSet<TravelPackageItem> TravelPackageItems => Set<TravelPackageItem>();
    public DbSet<TravelPackageImage> TravelPackageImages => Set<TravelPackageImage>();
    public DbSet<PackageBookingRequest> PackageBookingRequests =>
        Set<PackageBookingRequest>();
    public DbSet<PackageBookingOffer> PackageBookingOffers =>
        Set<PackageBookingOffer>();
    public DbSet<BookingPayment> BookingPayments => Set<BookingPayment>();
    public DbSet<PackageBookingMessage> PackageBookingMessages => Set<PackageBookingMessage>();
    public DbSet<PackageBookingTraveler> PackageBookingTravelers => Set<PackageBookingTraveler>();
    public DbSet<PackageBookingDocument> PackageBookingDocuments => Set<PackageBookingDocument>();
    public DbSet<BookingSupplierEvidence> BookingSupplierEvidence => Set<BookingSupplierEvidence>();
    public DbSet<BookingPayout> BookingPayouts => Set<BookingPayout>();
    public DbSet<BookingDispute> BookingDisputes => Set<BookingDispute>();

    public DbSet<AgencyReview> AgencyReviews => Set<AgencyReview>();

    // Subscription / Payment
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();

    // Public Trips
    public DbSet<PublicTripSnapshot> PublicTripSnapshots =>
        Set<PublicTripSnapshot>();

    public DbSet<SnapshotVersion> SnapshotVersions =>
        Set<SnapshotVersion>();

    public DbSet<TripInteraction> TripInteractions =>
        Set<TripInteraction>();

    public DbSet<TripRating> TripRatings =>
        Set<TripRating>();

    public DbSet<PublicTripView> PublicTripViews =>
        Set<PublicTripView>();

    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityPostMedia> CommunityPostMedia => Set<CommunityPostMedia>();
    public DbSet<CommunityPostComment> CommunityPostComments => Set<CommunityPostComment>();
    public DbSet<CommunityPostInteraction> CommunityPostInteractions => Set<CommunityPostInteraction>();

    // Chatbot tables
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages =>
        Set<ConversationMessage>();

    public DbSet<TravelKnowledge> TravelKnowledges =>
        Set<TravelKnowledge>();

    public DbSet<ChatFolder> ChatFolders => Set<ChatFolder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<PublicTripView>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasIndex(e => new { e.SnapshotVersionId, e.UserId, e.SessionId, e.ViewedAt });
            
            entity.HasOne(e => e.SnapshotVersion)
                  .WithMany(s => s.Views)
                  .HasForeignKey(e => e.SnapshotVersionId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });


        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.HasOne(e => e.User)
                .WithMany(user => user.Conversations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Folder)
                .WithMany(f => f.Conversations)
                .HasForeignKey(e => new { e.FolderId, e.UserId })
                .HasPrincipalKey(f => new { f.Id, f.UserId })
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ChatFolder>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasAlternateKey(e => new { e.Id, e.UserId });

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.HasOne(e => e.User)
                .WithMany(user => user.ChatFolders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
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

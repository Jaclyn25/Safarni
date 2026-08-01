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
    public DbSet<VisaApplication> VisaApplications => Set<VisaApplication>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

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

            entity.Ignore(e => e.Embedding);
        });
    }
}

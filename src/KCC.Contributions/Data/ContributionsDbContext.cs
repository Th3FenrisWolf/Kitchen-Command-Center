using Microsoft.EntityFrameworkCore;

namespace KCC.Contributions.Data;

public class ContributionsDbContext(DbContextOptions<ContributionsDbContext> options) : DbContext(options)
{
    public const int MaxTextLength = 4000;

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<CookNote> CookNotes => Set<CookNote>();

    public DbSet<CookedMark> CookedMarks => Set<CookedMark>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(review =>
        {
            review.ToTable("kccReview");
            review.HasKey(r => r.Id);
            review.Property(r => r.Id).HasColumnName("id");
            review.Property(r => r.VariantKey).HasColumnName("variantKey");
            review.Property(r => r.MemberKey).HasColumnName("memberKey");

            // SQLite has no decimal type, so EF would store the rating as text, which SQL cannot sum or order.
            // Half-star steps are exact as doubles.
            review.Property(r => r.Rating).HasColumnName("rating").HasConversion<double>();
            review.Property(r => r.Text).HasColumnName("text").HasMaxLength(MaxTextLength);
            review.Property(r => r.Created).HasColumnName("created");
            review.Property(r => r.Modified).HasColumnName("modified");
            review.HasIndex(r => new { r.VariantKey, r.MemberKey }).IsUnique();
        });

        modelBuilder.Entity<CookNote>(note =>
        {
            note.ToTable("kccCookNote");
            note.HasKey(n => n.Id);
            note.Property(n => n.Id).HasColumnName("id");
            note.Property(n => n.VariantKey).HasColumnName("variantKey");
            note.Property(n => n.MemberKey).HasColumnName("memberKey");
            note.Property(n => n.Text).HasColumnName("text").HasMaxLength(MaxTextLength).IsRequired();
            note.Property(n => n.Created).HasColumnName("created");
            note.Property(n => n.Modified).HasColumnName("modified");
            note.HasIndex(n => n.VariantKey);
        });

        modelBuilder.Entity<CookedMark>(cooked =>
        {
            cooked.ToTable("kccCookedMark");
            cooked.HasKey(c => c.Id);
            cooked.Property(c => c.Id).HasColumnName("id");
            cooked.Property(c => c.VariantKey).HasColumnName("variantKey");
            cooked.Property(c => c.MemberKey).HasColumnName("memberKey");
            cooked.Property(c => c.Created).HasColumnName("created");
            cooked.HasIndex(c => new { c.VariantKey, c.MemberKey }).IsUnique();
        });
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // 1. Add this namespace
using Microsoft.EntityFrameworkCore;
using SurveyApp.Models;

namespace SurveyApp.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Service> Services { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<SurveyType> SurveyTypes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<SurveySubmission> SurveySubmissions { get; set; }
    public DbSet<QuestionResponse> QuestionResponses { get; set; }
    public DbSet<SurveyCategory> SurveyCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SurveySubmission>().ToTable("survey_submissions");
        modelBuilder.Entity<QuestionResponse>().ToTable("question_responses");
        modelBuilder.Entity<Technician>().ToTable("technicians");
        modelBuilder.Entity<Service>().ToTable("services");
        modelBuilder.Entity<Question>().ToTable("questions");
        modelBuilder.Entity<SurveyCategory>().ToTable("survey_categories");
        modelBuilder.Entity<SurveyType>().ToTable("survey_types");

        modelBuilder.Entity<Question>()
            .HasOne(q => q.SurveyCategory)
            .WithMany()
            .HasForeignKey(q => q.survey_categories_id);
    }
}
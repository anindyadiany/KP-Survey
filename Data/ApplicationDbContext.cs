using Microsoft.EntityFrameworkCore;
using SurveyApp.Models;

namespace SurveyApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Map these to your MySQL table names exactly as written in your SQL file
    public DbSet<Service> Services { get; set; }
    public DbSet<Technician> Technicians { get; set; }
    public DbSet<SurveyType> SurveyTypes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<SurveySubmission> SurveySubmissions { get; set; }
    public DbSet<QuestionResponse> QuestionResponses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SurveySubmission>().ToTable("survey_submissions");
        modelBuilder.Entity<QuestionResponse>().ToTable("question_responses");
        modelBuilder.Entity<Technician>().ToTable("technicians");
        modelBuilder.Entity<Service>().ToTable("services");
        modelBuilder.Entity<Question>().ToTable("questions");
        modelBuilder.Entity<SurveyCategory>().ToTable("survey_categories");
        modelBuilder.Entity<SurveyType>().ToTable("survey_types");
    }
}
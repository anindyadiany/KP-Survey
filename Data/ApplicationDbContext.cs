using Microsoft.EntityFrameworkCore;
using SurveyApp.Models;

namespace SurveyApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Survey> Surveys { get; set; }
}
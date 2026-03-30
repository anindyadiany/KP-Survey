namespace SurveyApp.Models;

public class SurveySubmission
{
    public int id { get; set; }
    public DateTime submitted_at { get; set; } = DateTime.Now;
    public int survey_types_id { get; set; }
    public string? respondent_name { get; set; }
    public string? respondent_email { get; set; }
    public string? respondent_function { get; set; }
}
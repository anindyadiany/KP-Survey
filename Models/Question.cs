namespace SurveyApp.Models;

public class Question
{
    public int id { get; set; }
    public string question_body { get; set; } = string.Empty;
    public string input_type { get; set; } = string.Empty;
    public bool is_active { get; set; }
    public int survey_categories_id { get; set; }
    public int survey_types_id { get; set; }
}
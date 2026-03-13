namespace SurveyApp.Models;

public class QuestionResponse
{
    public int id { get; set; }
    public int? rating_score { get; set; } 
    public string? text_response { get; set; } 
    public int questions_id { get; set; }
    public int survey_submissions_id { get; set; }
    public int survey_categories_id { get; set; }
    public int? technicians_id { get; set; } 
    public int? service_id { get; set; } 
}
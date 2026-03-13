namespace SurveyApp.Models;

public class Technician
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string photo_path { get; set; } = string.Empty;
    public int service_id { get; set; }
}
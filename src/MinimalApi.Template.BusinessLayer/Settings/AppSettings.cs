namespace MinimalApi.Template.BusinessLayer.Settings;

public class AppSettings
{
    public string[] SupportedCultures { get; init; } = ["en"];
    public bool CachingEnabled { get; set; }
}
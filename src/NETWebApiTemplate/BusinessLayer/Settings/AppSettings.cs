namespace NETWebApiTemplate.BusinessLayer.Settings;

public class AppSettings
{
    public string[] SupportedCultures { get; init; } = ["en"];
    public string JWTSectionName { get; set; } = string.Empty;
}
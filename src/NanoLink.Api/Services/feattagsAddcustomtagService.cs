namespace NanoLink.Api.Services;

public interface IfeattagsAddcustomtagService
{
    bool ExecuteFeature(string input, out string result);
}

public class feattagsAddcustomtagService : IfeattagsAddcustomtagService
{
    public bool ExecuteFeature(string input, out string result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = string.Empty;
            return false;
        }
        result = $"Processed: {input}";
        return true;
    }
}

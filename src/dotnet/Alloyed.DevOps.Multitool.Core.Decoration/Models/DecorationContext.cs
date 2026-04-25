namespace Alloyed.DevOps.Multitool.Core.Decoration.Models;

public sealed class DecorationContext
{
    public DecorationContext(string operation, IDictionary<string, string>? tags = null)
    {
        Operation = string.IsNullOrWhiteSpace(operation) ? "unknown" : operation;
        Tags = tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase);
    }

    public string Operation { get; }

    public IDictionary<string, string> Tags { get; }

    public string? GetTag(string key)
    {
        return Tags.TryGetValue(key, out var value) ? value : null;
    }

    public void SetTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        Tags[key] = value;
    }
}

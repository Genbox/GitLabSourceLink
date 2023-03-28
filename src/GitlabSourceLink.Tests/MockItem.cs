using System.Collections;
using Microsoft.Build.Framework;

namespace Genbox.GitlabSourceLink.Tests;

public sealed class MockItem : ITaskItem2
{
    private readonly Dictionary<string, string> _escapedMetadata = new Dictionary<string, string>();

    public static string AdjustSeparators(string path) => Path.DirectorySeparatorChar == '/' ? path.Replace('\\', '/') : path;

    public MockItem(string spec, params KeyValuePair<string, string>[] metadata)
    {
        // msbuild normalizes paths on non-Windows like so:
        ItemSpec = AdjustSeparators(spec);

        foreach ((string name, string value) in metadata)
        {
            SetMetadata(name, value);
        }
    }

    public string ItemSpec { get; set; }

    public string GetMetadataValueEscaped(string metadataName) => _escapedMetadata.TryGetValue(metadataName, out string? value) ? value : string.Empty;

    public string GetMetadata(string metadataName) => Microsoft.Build.Evaluation.ProjectCollection.Unescape(GetMetadataValueEscaped(metadataName));

    public void SetMetadata(string metadataName, string? metadataValue) => _escapedMetadata[metadataName] = metadataValue ?? string.Empty;

    public void SetMetadataValueLiteral(string metadataName, string metadataValue) => SetMetadata(metadataName, Microsoft.Build.Evaluation.ProjectCollection.Escape(metadataValue));

    public void RemoveMetadata(string metadataName) => throw new NotImplementedException();

    public ICollection MetadataNames => throw new NotImplementedException();

    public int MetadataCount => throw new NotImplementedException();

    public string EvaluatedIncludeEscaped { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IDictionary CloneCustomMetadata() => throw new NotImplementedException();

    public void CopyMetadataTo(ITaskItem destinationItem) => throw new NotImplementedException();

    public IDictionary CloneCustomMetadataEscaped() => throw new NotImplementedException();
}
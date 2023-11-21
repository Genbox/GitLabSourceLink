using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Genbox.GitLabSourceLink;

public class GenerateSourceLinkFile : Task
{
    [Required]
    public string OutputFile { get; set; }

    [Required]
    public ITaskItem[] SourceRoots { get; set; }

    [Required]
    public ITaskItem[] SourceFiles { get; set; }

    [Required]
    public string GitLabUrl { get; set; }

    [Required]
    public string Revision { get; set; }

    public ITaskItem[] UntrackedFiles { get; set; }

    [Output]
    public string Root { get; set; }

    public override bool Execute()
    {
        //Find the one that we need for scm
        ITaskItem? taskItem = Array.Find(SourceRoots, x => !string.IsNullOrEmpty(x.GetMetadata("SourceControl")));

        if (taskItem == null)
            throw new InvalidOperationException("Unable to find a source root with SourceControl set");

        string root = taskItem.GetMetadata("Identity");
        Root = root;

        IEnumerable<KeyValuePair<string, string>> items = GenerateItems(root);

        StringBuilder result = new StringBuilder();
        result.Append("{\"documents\":{");

        bool isFirst = true;

        foreach (KeyValuePair<string, string> item in items)
        {
            if (!isFirst) result.Append(',');

            isFirst = false;

            result.Append('"');
            result.Append(EscapeString(item.Key));
            result.Append('"');
            result.Append(':');
            result.Append('"');
            result.Append(EscapeString(item.Value));
            result.Append('"');
        }

        result.Append("}}");

        File.WriteAllText(OutputFile, result.ToString());

        return !Log.HasLoggedErrors;
    }

    private IEnumerable<KeyValuePair<string, string>> GenerateItems(string sourceRoot)
    {
        Uri uri = new Uri(GitLabUrl);

        UriBuilder uriBuilder = new UriBuilder();
        uriBuilder.Scheme = uri.Scheme;
        uriBuilder.Host = uri.Host;

        if (!uri.IsDefaultPort)
            uriBuilder.Port = uri.Port;

        uriBuilder.Query = $"?ref={Revision}";

        string repo = uri.AbsolutePath.Substring(1, uri.AbsolutePath.Length - 5);

        HashSet<string> untracked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ITaskItem item in UntrackedFiles)
        {
            string meta = item.GetMetadata("FullPath");

            if (!string.IsNullOrEmpty(meta))
                untracked.Add(meta);
        }

        foreach (ITaskItem file in SourceFiles)
        {
            string fullPath = file.GetMetadata("FullPath");

            if (untracked.Contains(fullPath))
                continue;

            string relativePath = GetRelativePath(sourceRoot, fullPath).Replace('\\', '/');
            uriBuilder.Path = $"/api/v4/projects/{WebUtility.UrlEncode(repo)}/repository/files/{WebUtility.UrlEncode(relativePath)}/raw";

            yield return new KeyValuePair<string, string>("/_/" + relativePath, uriBuilder.ToString());
        }
    }

    private static string GetRelativePath(string relativeTo, string path)
    {
#if NETSTANDARD
        return MakeRelativePath(relativeTo, path);
#else
        return Path.GetRelativePath(relativeTo, path);
#endif
    }

#if NETSTANDARD
    private static string MakeRelativePath(string fromPath, string toPath)
    {
        if (string.IsNullOrEmpty(fromPath))
            throw new ArgumentNullException(nameof(fromPath));

        if (string.IsNullOrEmpty(toPath))
            throw new ArgumentNullException(nameof(toPath));

        Uri fromUri = new Uri(fromPath);
        Uri toUri = new Uri(toPath);

        if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        return relativePath;
    }
#endif

    private string EscapeString(string input)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                {
                    if (char.IsControl(c))
                        sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:X4}", (int)c);
                    else
                        sb.Append(c);

                    break;
                }
            }
        }
        return sb.ToString();
    }
}
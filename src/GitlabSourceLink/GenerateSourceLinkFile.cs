using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Genbox.GitlabSourceLink;

public class GenerateSourceLinkFile : Task
{
    [Required]
    public ITaskItem[] SourceRoots { get; set; }

    [Required]
    public string OutputFile { get; set; }

    public override bool Execute()
    {
        System.Diagnostics.Debugger.Launch();

        string? content = GenerateSourceLinkContent();

        if (content != null)
            WriteSourceLinkFile(content);

        return !Log.HasLoggedErrors;
    }

    internal string? GenerateSourceLinkContent()
    {
        bool success = true;
        foreach (ITaskItem root in SourceRoots)
        {
            string mappedPath = root.GetMetadata(Names.SourceRoot.MappedPath);
            bool isMapped = !string.IsNullOrEmpty(mappedPath);
            string localPath = isMapped ? mappedPath : root.ItemSpec;

            if (!localPath.EndsWithSeparator())
            {
                Log.LogError("{0} must end with a directory separator: '{1}'", isMapped ? Names.SourceRoot.MappedPathFullName : Names.SourceRoot.Name, localPath);
                success = false;
                continue;
            }

            if (localPath.Contains('*'))
            {
                Log.LogError("{0} must not contain wildcard '*': '{1}'", isMapped ? Names.SourceRoot.MappedPathFullName : Names.SourceRoot.Name, localPath);
                success = false;
                continue;
            }

            string? url = root.GetMetadata(Names.SourceRoot.SourceLinkUrl);
            if (string.IsNullOrEmpty(url))
            {
                // Do not report any diagnostic. If the source root comes from source control a warning has already been reported.
                // SourceRoots can be specified by the project to make other features like deterministic paths, and they don't need source link URL.
                continue;
            }

            if (url.Count(c => c == '*') != 1)
            {
                Log.LogError("{0} must not contain wildcard '*': '{1}'", Names.SourceRoot.SourceLinkUrlFullName, url);
                success = false;
            }
        }

        if (!success)
            return null;

        Log.LogWarning("Source control information is not available - the generated source link is empty.");

        return "";
    }

    private void WriteSourceLinkFile(string content)
    {
        try
        {
            if (File.Exists(OutputFile))
            {
                string originalContent = File.ReadAllText(OutputFile);

                // Don't rewrite the file if the contents are the same
                if (originalContent == content)
                    return;
            }

            File.WriteAllText(OutputFile, content);
        }
        catch (Exception e)
        {
            Log.LogError("Error writing to source link file '{0}': {1}", OutputFile, e.Message);
        }
    }
}
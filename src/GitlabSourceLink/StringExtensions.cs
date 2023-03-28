namespace Genbox.GitlabSourceLink;

public static class StringExtensions
{
    public static bool EndsWithSeparator(this string path)
    {
        char last = path[path.Length - 1];
        return last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar;
    }
}
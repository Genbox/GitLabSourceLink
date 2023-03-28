namespace Genbox.GitlabSourceLink.Tests;

public class GeneralTests
{
    [Fact]
    public void Empty()
    {
        MockEngine engine = new MockEngine();

        GenerateSourceLinkFile task = new GenerateSourceLinkFile()
        {
            BuildEngine = engine,
            SourceRoots = Array.Empty<MockItem>(),
        };

        string? content = task.GenerateSourceLinkContent();

        // AssertEx.AssertEqualToleratingWhitespaceDifferences("WARNING : " + string.Format(Resources.SourceControlInformationIsNotAvailableGeneratedSourceLinkEmpty), engine.Log);
        // AssertEx.AreEqual(@"{""documents"":{}}", content);
    }
}
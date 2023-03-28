﻿using System.Collections;
using System.Text;
using Microsoft.Build.Framework;

namespace Genbox.GitlabSourceLink.Tests;

public sealed class MockEngine : IBuildEngine
{
    private StringBuilder _log = new StringBuilder();
    public MessageImportance MinimumMessageImportance = MessageImportance.Low;

    public string Log
    {
        set { _log = new StringBuilder(value); }
        get { return _log.ToString(); }
    }

    public void LogErrorEvent(BuildErrorEventArgs eventArgs)
    {
        _log.Append("ERROR ");
        _log.Append(eventArgs.Code);
        _log.Append(": ");
        _log.Append(eventArgs.Message);
        _log.AppendLine();
    }

    public void LogWarningEvent(BuildWarningEventArgs eventArgs)
    {
        _log.Append("WARNING ");
        _log.Append(eventArgs.Code);
        _log.Append(": ");
        _log.Append(eventArgs.Message);
        _log.AppendLine();
    }

    public void LogCustomEvent(CustomBuildEventArgs eventArgs)
    {
        _log.AppendLine(eventArgs.Message);
    }

    public void LogMessageEvent(BuildMessageEventArgs eventArgs)
    {
        _log.AppendLine(eventArgs.Message);
    }

    public string ProjectFileOfTaskNode => "";
    public int ColumnNumberOfTaskNode => 0;
    public int LineNumberOfTaskNode => 0;
    public bool ContinueOnError => true;

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new NotImplementedException();
}
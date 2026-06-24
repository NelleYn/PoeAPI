using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ExileCore.Shared;
public class MsBuildLogger : Logger
{
    public IList<BuildTarget> Targets { get; private set; }
    public IList<BuildError> Errors { get; private set; }
    public IList<BuildWarning> Warnings { get; private set; }
    public IList<string> BuildDetails { get; private set; }

    public override void Initialize(IEventSource eventSource)
    {
    }

    private void EventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
    {
    }

    private void EventSource_TargetFinished(object sender, TargetFinishedEventArgs e)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private void EventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private void EventSource_WarningRaised(object sender, BuildWarningEventArgs e)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private void EventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
    {
    }
}
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ExileCore.Shared;
public class MsBuildLogger : Logger
{
    public IList<BuildTarget> Targets
    {
        [CompilerGenerated]
        get
        {
            return (IList<BuildTarget>)this;
        }

        [CompilerGenerated]
        private set
        {
        }
    }

    public IList<BuildError> Errors
    {
        [CompilerGenerated]
        get
        {
            return (IList<BuildError>)this;
        }

        [CompilerGenerated]
        private set
        {
        }
    }

    public IList<BuildWarning> Warnings
    {
        [CompilerGenerated]
        get
        {
            return (IList<BuildWarning>)this;
        }

        [CompilerGenerated]
        private set
        {
        }
    }

    public IList<string> BuildDetails
    {
        [CompilerGenerated]
        get
        {
            return (IList<string>)this;
        }

        [CompilerGenerated]
        private set
        {
        }
    }

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
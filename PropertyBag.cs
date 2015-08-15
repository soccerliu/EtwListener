// Downloaded from http://archive.msdn.microsoft.com/EventTraceWatcher/Release/ProjectReleases.aspx?ReleaseId=2333
// licensed under MSDN Code Gallery Licenses / Source code files are governed by the MICROSOFT PUBLIC LICENSE (Ms-PL)
//
// Note that a slightly different copy is also checked-in in other source depot locations
// - //depot/win8_gdr/base/fs/remotefs/srv/xperf/smbxparse/...
// - //depot/win8_gdr/basetest/clientperf/diagnosis/perftrack/perftrackrealtime/PerfTrackRealTime/...
// - etc.

//-----------------------------------------------------------------------------
// Author: Daniel Vasquez Lopez
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Samples.Eventing
{

    [Serializable]
    public sealed class PropertyBag : Dictionary<string, object>
    {
        public PropertyBag()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }

}
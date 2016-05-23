using System;
using NLog;
using Org.Apache.CuratorNet.Client.Drivers;

namespace Org.Apache.CuratorNet.Client.Utils
{
    /// <summary>
    /// Default tracer driver
    /// </summary>
    public class DefaultTracerDriver : ITracerDriver
    {
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    public void addTrace(String name, int timeMs)
    {
        if (_log.IsTraceEnabled)
        {
            _log.Trace("Trace: " + name + " - " + timeMs + " ms");
        }
    }

    public void addCount(String name, int increment)
    {
        if (_log.IsTraceEnabled)
        {
            _log.Trace("Counter " + name + ": " + increment);
        }
    }
}
}

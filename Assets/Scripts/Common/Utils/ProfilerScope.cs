using System;
using UnityEngine.Profiling;

namespace Common.Utils
{
public struct ProfilerScope : IDisposable
{
    public ProfilerScope(string name)
    {
        Profiler.BeginSample(name);
    }

    public void Dispose()
    {
        Profiler.EndSample();
    }
}
}
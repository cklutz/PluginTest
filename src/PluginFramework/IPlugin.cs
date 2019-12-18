using System;

namespace PluginFramework
{
    public interface IPlugin : IDisposable
    {
        string Name { get; }
    }
}

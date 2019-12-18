using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace PluginFramework
{
    public sealed class PluginManager : IDisposable
    {
        private bool m_disposed;
        private readonly object m_pluginsLock = new object();
        private readonly Dictionary<string, (IPlugin Instance, PluginContext Context)> m_plugins
            = new Dictionary<string, (IPlugin Instance, PluginContext Context)>(StringComparer.OrdinalIgnoreCase);

        public IPlugin Load(string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException(nameof(assemblyPath));
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentException("Assembly path is empty", nameof(assemblyPath));

            lock (m_pluginsLock)
            {
                assemblyPath = Path.GetFullPath(assemblyPath);
                EnsureUniqueLocation(assemblyPath);

                IPlugin instance = null;
                var context = new PluginContext(assemblyPath, new[] { GetType() });
                try
                {
                    var assembly = context.LoadFromAssemblyPath(assemblyPath);
                    var pluginType = FindPluginType(assembly, assemblyPath);
                    instance = (IPlugin)Activator.CreateInstance(pluginType);
                    EnsureUniqueName(instance.Name);

                    m_plugins.Add(assemblyPath, (instance, context));

                    return instance;
                }
                catch (Exception)
                {
                    instance?.Dispose();
                    context.Unload();
                    throw;
                }
            }
        }

        private void EnsureUniqueLocation(string assemblyPath)
        {
            // Note: this does not care about links, junctions, etc.
            // Would have to use native APIs for that.
            if (m_plugins.ContainsKey(assemblyPath))
            {
                throw new ArgumentException($"The assembly '{assemblyPath}' is already loaded");
            }
        }

        private Type FindPluginType(Assembly assembly, string assemblyPath)
        {
            var candidates = assembly.GetTypes().Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsPublic &&
                t.GetInterfaces().Any(i => i == typeof(IPlugin)));

            if (!candidates.Any())
                throw new ArgumentException($"Assembly {assemblyPath} contains no {typeof(IPlugin)} type");
            if (candidates.Count() > 1)
                throw new ArgumentException($"Assembly {assemblyPath} contains multiple {typeof(IPlugin)} types");

            return candidates.First();
        }

        private void EnsureUniqueName(string name)
        {
            var existing = m_plugins.Where(p => p.Value.Instance.Name == name);
            if (existing.Any())
            {
                throw new ArgumentException($"A plugin with the name '{name}' is already loaded " +
                    $" from assembly '{existing.First().Key}'");
            }
        }

        public IEnumerable<string> LoadedPlugins
        {
            get
            {
                lock (m_pluginsLock)
                {
                    return m_plugins.Values.Select(v => v.Instance.Name);
                }
            }
        }

        public IPlugin FindPlugin(string name)
        {
            lock (m_pluginsLock)
            {
                var pluginData = GetPlugin(name, true);
                return pluginData.Instance;
            }
        }

        public bool TryFindPlugin(string name, out IPlugin plugin)
        {
            lock (m_pluginsLock)
            {
                var pluginData = GetPlugin(name, false);
                if (pluginData.Instance == null)
                {
                    plugin = null;
                    return false;
                }

                plugin = pluginData.Instance;
                return true;
            }
        }

        public void Unload(IPlugin plugin, bool wait = false)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            lock (m_pluginsLock)
            {
                var pluginData = GetPlugin(plugin.Name, true);

                try
                {
                    pluginData.Instance.Dispose();
                }
                finally
                {
                    try
                    {
                        pluginData.Context.Unload();
                        if (wait)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                    finally
                    {
                        m_plugins.Remove(pluginData.Context.AssemblyPath);
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (m_pluginsLock)
            {
                if (!m_disposed)
                {
                    foreach (var entry in m_plugins)
                    {
                        try
                        {
                            entry.Value.Instance.Dispose();
                        }
                        finally
                        {
                            entry.Value.Context.Unload();
                        }
                    }

                    m_disposed = true;
                }
            }
        }

        private (IPlugin Instance, PluginContext Context) GetPlugin(string name, bool throwOnError)
        {
            var entry = m_plugins.Where(v => v.Value.Instance.Name == name);
            if (entry.Any())
            {
                if (throwOnError)
                {
                    throw new ArgumentException($"The plugin '{name}' is not loaded", nameof(name));
                }
                return (null, null);
            }

            return entry.First().Value;
        }

        internal class PluginContext : AssemblyLoadContext
        {
            private readonly Dictionary<string, Assembly> m_shareAssemblies = new Dictionary<string, Assembly>(
                StringComparer.OrdinalIgnoreCase);
            private readonly string m_baseDirectory;

            public PluginContext(string assemblyPath, IEnumerable<Type> sharedTypes)
                : base(assemblyPath, isCollectible: true)
            {
                if (assemblyPath == null)
                    throw new ArgumentNullException(nameof(assemblyPath));

                m_baseDirectory = Path.GetDirectoryName(assemblyPath);

                if (sharedTypes != null)
                {
                    foreach (var type in sharedTypes)
                    {
                        m_shareAssemblies[Path.GetFileName(type.Assembly.Location)] = type.Assembly;
                    }
                }

                AssemblyPath = assemblyPath;
            }

            public string AssemblyPath { get; }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                string fileName = assemblyName.Name + ".dll";

                if (m_shareAssemblies.TryGetValue(fileName, out var sharedAssembly))
                {
                    return sharedAssembly;
                }

                string fullPath = Path.Combine(m_baseDirectory, fileName);

                if (File.Exists(fullPath))
                {
                    return LoadFromAssemblyPath(fullPath);
                }

                return base.Load(assemblyName);
            }
        }
    }
}


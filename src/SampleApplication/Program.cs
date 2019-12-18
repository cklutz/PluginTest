using PluginFramework;
using System;

namespace SampleApplication
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                using (var pluginManager = new PluginManager())
                {
                    foreach (var path in args)
                    {
                        Console.WriteLine("Loading plugin from {0}", path);
                        pluginManager.Load(path);
                    }

                    Console.WriteLine();
                    Console.WriteLine("Loaded plugins:");
                    foreach (var loaded in pluginManager.LoadedPlugins)
                    {
                        Console.WriteLine(loaded);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return ex.HResult;
            }

            return 0;
        }
    }
}

using Newtonsoft.Json;
using PluginFramework;
using System;

namespace SamplePlugin
{
    public class MyPluginImpl : IPlugin
    {
        public MyPluginImpl()
        {
            Console.WriteLine("Creating: " + Name + ", referencing " + typeof(JsonConvert).Assembly.FullName);
        }

        public void Dispose()
        {
            Console.WriteLine("Diposing: " + Name);
        }

        public string Name => "MyPlugin";
    }
}

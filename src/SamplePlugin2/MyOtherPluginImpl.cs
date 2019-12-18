using Newtonsoft.Json;
using PluginFramework;
using System;

namespace SamplePlugin2
{
    public class MyOtherPluginImpl : IPlugin
    {
        public MyOtherPluginImpl()
        {
            Console.WriteLine("Creating: " + Name + ", referencing " + typeof(JsonConvert).Assembly.FullName);
        }

        public string Name => "MyOtherPlugin";

        public void Dispose()
        {
            Console.WriteLine("Dispose: " + Name);
        }
    }
}

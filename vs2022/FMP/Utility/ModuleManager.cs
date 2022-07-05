using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class ModuleManager
    {
        private Dictionary<string, Assembly> assemblyMap = new Dictionary<string, Assembly>();

        public async Task<Assembly> Load(string _path, HttpClient _httpClient)
        {
            Assembly assembly;
            string name = Path.GetFileName(_path);
            if(!assemblyMap.TryGetValue(name, out assembly))
            {
                var bytes = await _httpClient.GetByteArrayAsync(_path);
                assembly = Assembly.Load(bytes);
                assemblyMap[name] = assembly;
            }
            return assembly;
        }

        public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = assemblyMap[args.Name.Remove(args.Name.IndexOf(',')) + ".dll"];
            return assembly;
        }
    }
}

using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class ModuleRouter
    {
        private class Module
        {
            public string path { get; set; }
            public string ns { get; set; }
            public string[] assemblies { get; set; }
        }

        private class ModuleConfig
        {
            public Module[] modules { get; set; }
        }

        private ModuleConfig config_;

        private Dictionary<string, Assembly> assemblyMap_ = new Dictionary<string, Assembly>();
        private Dictionary<string, List<Assembly>> routerCache_ = new Dictionary<string, List<Assembly>>();

        public async Task<List<Assembly>> Route(string _path, HttpClient _httpClient, GrpcChannel _channel, Framework _framework, Logger _logger)
        {
            // 加载配置文件
            if (null == config_)
            {
                _logger.Debug("load modules.json ......");
                config_ = await _httpClient.GetFromJsonAsync<ModuleConfig>("data/modules.json");
                if (null == config_)
                {
                    return new List<Assembly>();
                }
                foreach (var module in config_.modules)
                {
                    routerCache_[module.path] = null;
                }
            }

            List<Assembly> assemblies;
            // 不是模块的路径
            if (!routerCache_.TryGetValue(_path, out assemblies))
                return new List<Assembly>();

            // 是模块的路径，并已经加载过
            if (null != assemblies)
                return new List<Assembly>();

            foreach (var module in config_.modules)
            {
                if (!module.path.Equals(_path))
                    continue;

                assemblies = new List<Assembly>();
                foreach (var assemblyName in module.assemblies)
                {
                    _logger.Debug($"load {assemblyName} ......");
                    var assembly = await load($"modules/{assemblyName}", _httpClient, _logger);
                    if (null == assembly)
                    {
                        _logger.Error($"load {assemblyName} failed");
                        continue;
                    }
                    _logger.Debug($"load {assemblyName} success");
                    assemblies.Add(assembly);
                    if (assemblyName.EndsWith("-mvcs.dll"))
                        activateModule(assembly, module.ns, _channel, _framework, _logger);
                }
                routerCache_[module.path] = assemblies;
                break;
            }
            return assemblies;
        }

        public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = assemblyMap_[args.Name.Remove(args.Name.IndexOf(',')) + ".dll"];
            return assembly;
        }

        private async Task<Assembly> load(string _path, HttpClient _httpClient, Logger _logger)
        {
            Assembly assembly;
            string name = Path.GetFileName(_path);
            if (!assemblyMap_.TryGetValue(name, out assembly))
            {
                try
                {
                    var bytes = await _httpClient.GetByteArrayAsync(_path);
                    assembly = Assembly.Load(bytes);
                    assemblyMap_[name] = assembly;
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex);
                }
            }
            return assembly;
        }

        private void activateModule(Assembly _assembly, string _namespace, GrpcChannel _channel, Framework _framework, Logger _logger)
        {
            _logger.Debug($"new Options ...");
            string optionsClassName = $"{_namespace}.LIB.MVCS.Options";
            object optionsInstance = _assembly.CreateInstance(optionsClassName);
            if(null == optionsInstance)
            {
                _logger.Error("CreateInstance failed");
                return;
            }
            Type optionsType = _assembly.GetType(optionsClassName);
            if(null == optionsType)
            {
                _logger.Error($"Type:{optionsClassName} not found in {_assembly.FullName}");
                return;
            }
            MethodInfo methodSetChannel = optionsType.GetMethod("setChannel");
            if (null == methodSetChannel)
            {
                _logger.Error($"Method:setChannel not found in {_assembly.FullName}");
                return;
            }
            _logger.Debug($"invoke {methodSetChannel} with ({_channel})");
            methodSetChannel.Invoke(optionsInstance, new object[] { _channel });

            _logger.Debug($"new Entry ...");
            string entryClassName = $"{_namespace}.LIB.MVCS.Entry";
            object entryInstance = _assembly.CreateInstance(entryClassName);
            if (null == entryInstance)
            {
                _logger.Error("CreateInstance failed");
                return;
            }
            Type entryType = _assembly.GetType(entryClassName);
            if (null == entryType)
            {
                _logger.Error($"Type:{entryClassName} not found in {_assembly.FullName}");
                return;
            }
            MethodInfo methodInject = entryType.GetMethod("Inject");
            if (null == methodInject)
            {
                _logger.Error($"Method:Inject not found in {_assembly.FullName}");
                return;
            }
            _logger.Debug($"invoke {methodInject} with ({_framework}, {optionsInstance})");
            methodInject.Invoke(entryInstance, new object[] { _framework, optionsInstance });
            MethodInfo methodRegister = entryType.GetMethod("DynamicRegister");
            if (null == methodRegister)
            {
                _logger.Error($"Method:DynamicRegister not found in {_assembly.FullName}");
                return;
            }
            _logger.Debug($"invoke {methodRegister} with ({_logger})");
            methodRegister.Invoke(entryInstance, new object[] { _logger });
            IUserData userData = entryInstance as IUserData;
            _logger.Trace($"push entry into framework");
            _framework.PushUserData(entryClassName, userData);
        }
    }
}

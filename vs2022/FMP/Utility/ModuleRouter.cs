/*
 * 延迟加载技术参见 https://learn.microsoft.com/zh-cn/aspnet/core/blazor/webassembly-lazy-load-assemblies
 */
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
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
        public class Module
        {
            public string org { get; set; }
            public string name { get; set; }
            public string version { get; set; }
            public string grpc { get; set; }
            public List<string> pages { get; set; } = new List<string>();
        }

        public class ModuleConfig
        {
            public Module[] modules { get; set; }
        }

        private ModuleConfig config_;

        private Dictionary<string, Assembly> assemblyMap_ = new();
        private List<string> paths_ = new();
        private Dictionary<string, string> permissionS_ = new();

        public async Task<List<Assembly>> Route(string _path, RuntimeScalingManager _scalingMgr, Framework _framework, Logger _logger)
        {
            // 加载配置文件
            if (null == config_)
            {
                _logger.Debug("load modules.json ......");
                if (_scalingMgr.settings.Active)
                {
                    config_ = _scalingMgr.moduleConfig;
                }
                else
                {
                    config_ = await _scalingMgr.internalClient.GetFromJsonAsync<ModuleConfig>("data/modules.json");
                }

                if (null == config_)
                {
                    return new List<Assembly>();
                }
                foreach (var module in config_.modules)
                {
                    foreach (var page in module.pages)
                    {
                        string path = string.Format("{0}/{1}/{2}", module.org.ToLower(), module.name.ToLower(), page.ToLower());
                        paths_.Add(path);
                    }
                }
            }

            // 不是模块的路径
            if (!paths_.Contains(_path))
                return new List<Assembly>();

            List<Assembly> assemblies = new List<Assembly>();

            // 加载路径对应的程序集
            foreach (var module in config_.modules)
            {
                var page = module.pages.Find((_item) =>
                {
                    return _path.Equals(string.Format("{0}/{1}/{2}", module.org.ToLower(), module.name.ToLower(), _item.ToLower()));
                });
                if (null == page)
                    continue;

                try
                {
                    // 加载程序集到内存
                    assemblies = await load(module, _scalingMgr, _framework, _logger);
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex);
                }
                break;
            }
            return assemblies;
        }

        public void UpdatePermissionS(Dictionary<string, string> _permissionS)
        {
            permissionS_ = _permissionS;
        }

        public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = assemblyMap_[args.Name.Remove(args.Name.IndexOf(',')) + ".dll"];
            Console.WriteLine("Resolve {0} {1}", args.Name, null == assembly ? "failure" : "success");
            return assembly;
        }

        private async Task<List<Assembly>> load(Module _module, RuntimeScalingManager _scalingMgr, Framework _framework, Logger _logger)
        {
            string[] dlls = new string[] {
                    string.Format("fmp-{0}-{1}-lib-proto.dll", _module.org.ToLower(), _module.name.ToLower()),
                    string.Format("fmp-{0}-{1}-lib-bridge.dll", _module.org.ToLower(), _module.name.ToLower()),
                    string.Format("fmp-{0}-{1}-lib-mvcs.dll", _module.org.ToLower(), _module.name.ToLower()),
                    string.Format("fmp-{0}-{1}-lib-razor.dll", _module.org.ToLower(), _module.name.ToLower()),
            };

            if (!_scalingMgr.settings.Active)
            {
                //TODO 处理Internal的模块
                throw new NotImplementedException();
            }

            string version = _module.version;
            if (_scalingMgr.settings.Environment.Equals("develop"))
                version = "develop";

            List<Assembly> assemblies = new List<Assembly>();
            foreach (var dll in dlls)
            {
                string filepath = $"fmp.repository/modules/{_module.org}/{_module.name}@{version}/{dll}";
                Assembly assembly;
                if (!assemblyMap_.TryGetValue(dll, out assembly))
                {
                    _logger.Debug($"load {dll} ......");
                    // 没有加载过
                    try
                    {
                        var bytes = await _scalingMgr.repositoryClient.GetByteArrayAsync(filepath);
                        assembly = Assembly.Load(bytes);
                        assemblyMap_[dll] = assembly;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"load {dll} failed");
                        _logger.Exception(ex);
                        continue;
                    }
                    if (dll.EndsWith("-mvcs.dll"))
                        activateModule(assembly, _module, _framework, _logger);
                    _logger.Debug($"load {dll} success");
                }
                assemblies.Add(assembly);
            }
            return assemblies;

        }

        private void activateModule(Assembly _assembly, Module _module, Framework _framework, Logger _logger)
        {
            var channel = GrpcChannel.ForAddress(_module.grpc, new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler()),
                MaxReceiveMessageSize = int.MaxValue,
                MaxSendMessageSize = int.MaxValue,
            });

            string ns = string.Format("{0}.FMP.MOD.{1}", _module.org, _module.name);
            _logger.Debug($"new Options ...");
            string optionsClassName = $"{ns}.LIB.MVCS.Options";
            object optionsInstance = _assembly.CreateInstance(optionsClassName);
            if (null == optionsInstance)
            {
                _logger.Error("CreateInstance failed");
                return;
            }
            Type optionsType = _assembly.GetType(optionsClassName);
            if (null == optionsType)
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
            _logger.Debug($"invoke {methodSetChannel} with ({channel})");
            methodSetChannel.Invoke(optionsInstance, new object[] { channel });
            MethodInfo methodSetPermissionS = optionsType.GetMethod("setPermissionS");
            if (null == methodSetPermissionS)
            {
                _logger.Error($"Method:setPermissionS not found in {_assembly.FullName}");
                return;
            }
            _logger.Debug($"invoke {methodSetPermissionS} with ({permissionS_.Count})");
            methodSetPermissionS.Invoke(optionsInstance, new object[] { permissionS_ });

            _logger.Debug($"new Entry ...");
            string entryClassName = $"{ns}.LIB.MVCS.Entry";
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
            // uid必须是default，razor库中使用的是此值
            methodRegister.Invoke(entryInstance, new object[] { "default", _logger });
            UserData userData = entryInstance as UserData;
            _logger.Trace($"push entry into framework");
            _framework.setUserData(entryClassName, userData);
        }
    }
}

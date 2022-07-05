using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class ModuleManager
    {
        /*
        public ConsoleLogger logger
        {
            get;
            set;
        }
        */
        public Framework framework
        {
            get;
            set;
        }

        public class Module
        {
            private object instance = null;
            private MethodInfo mi = null;
            private object[] param = null;

            public Module(object _instance, MethodInfo _methodInfo)
            {
                instance = _instance;
                mi = _methodInfo;
                param = new object[2];
            }

            public void Invoke(string _method, object[] _param)
            {
                param[0] = _method;
                param[1] = _param;
                mi.Invoke(instance, param);
            }
        }
        private Dictionary<string, Assembly> assemblyMap = new Dictionary<string, Assembly>();

        public ModuleManager()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(assemblyResolve);
        }

        public void Load()
        {
            string curDir = System.IO.Directory.GetCurrentDirectory();
            string modulesDir = Path.Combine(curDir, "modules");
            if (!Directory.Exists(modulesDir))
                return;

            foreach (string entry in Directory.GetFiles(modulesDir))
            {
                Assembly assembly = Assembly.LoadFile(entry);
                if (null == assembly)
                    continue;

                string filename = Path.GetFileName(entry);
                assemblyMap[filename] = assembly;
            }
        }

        public void Unload()
        {

        }

        public void Register()
        {
            foreach(string filename in assemblyMap.Keys)
            {
                string rootClass = "";
                if (filename.EndsWith("-mvcs.dll"))
                {
                    string ns = filename.Substring(0, filename.Length - ".module.dll".Length);
                    rootClass = string.Format("{0}.Entry", ns);
                }
                else if (filename.EndsWith(".wpf.dll"))
                {
                    string ns = filename.Substring(0, filename.Length - ".wpf.dll".Length);
                    rootClass = string.Format("{0}.ControlRoot", ns);
                }
                else
                {
                    continue;
                }

                Assembly assembly = assemblyMap[filename];
                object instance = assembly.CreateInstance(rootClass);
                Type t = assembly.GetType(rootClass);
                MethodInfo miInject = t.GetMethod("Inject");
                miInject.Invoke(instance, new object[] { framework });
                MethodInfo miRegister = t.GetMethod("Register");
                miRegister.Invoke(instance, null);
            }
        }

        public void Cancel()
        {
            //TODO Cancel
        }

        private Assembly assemblyResolve(object sender, ResolveEventArgs args)
        {
            return assemblyMap[args.Name.Remove(args.Name.IndexOf(',')) + ".dll"];
        }
    }
}

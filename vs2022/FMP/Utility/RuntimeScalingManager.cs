using AntDesign.ProLayout;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.APP.Blazor
{
    public class RuntimeScalingSettings
    {
        public bool Active { get; set; }
        public string Environment { get; set; }
        public string RepositoryAddress { get; set; }
        public string Grpc { get; set; }
        public string VendorAddress { get; set; }
    }

    /// <summary>
    /// 运行时伸缩
    /// </summary>
    public class RuntimeScalingManager
    {
        public HttpClient internalClient { get; private set; }
        public HttpClient repositoryClient { get; private set; }
        public HttpClient vendorClient { get; private set; }
        public RuntimeScalingSettings settings { get; set; }
        public MenuDataItem[] menuConfig { get; set; }
        public ModuleRouter.ModuleConfig moduleConfig { get; set; }
        public ProSettings proSettings { get; set; }
        public Logger logger { get; set; }
        public string vendor { get; set; }

        public RuntimeScalingManager()
        {
        }


        public void SetRepositoryHttpClient(Uri _uri)
        {
            repositoryClient = new HttpClient { BaseAddress = _uri };
        }

        public void SetInternalHttpClient(Uri _uri)
        {
            internalClient = new HttpClient { BaseAddress = _uri };
        }

        public void SetVendorHttpClient(Uri _uri)
        {
            vendorClient = new HttpClient { BaseAddress = _uri };
        }

        public async Task FetchData()
        {
            if (!string.IsNullOrWhiteSpace(vendor))
            {
                await fetchVendor();
                return;
            }
            await fetchAgents();
        }

        private async Task fetchVendor()
        {
            try
            {
                var vendorEntity = await vendorClient.GetFromJsonAsync<VendorEntity>(String.Format("fmp.vendor/blazor/{0}.json", vendor));
                // 解析menu_data
                string menuConfigJson = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(vendorEntity.MenuConfig));
                menuConfig = JsonConvert.DeserializeObject<MenuDataItem[]>(menuConfigJson);
                string moduleConfigJson = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(vendorEntity.ModulesConfig));
                moduleConfig = JsonConvert.DeserializeObject<ModuleRouter.ModuleConfig>(moduleConfigJson);
                string skinConfigJson = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(vendorEntity.SkinConfig));
                var tempProSettings = JsonConvert.DeserializeObject<ProSettings>(skinConfigJson);
                applyProSetting(tempProSettings);
            }
            catch (System.Exception ex)
            {
                logger.Exception(ex);
            }
        }

        private async Task fetchAgents()
        {
            List<MenuDataItem> menuDataItems = new List<MenuDataItem>();
            menuDataItems.Add(new MenuDataItem
            {
                Path = "/",
                Name = "Dashboard",
                Key = "dashboard",
                Icon = "dashboard",
            });
            List<ModuleRouter.Module> moduleS = new List<ModuleRouter.Module>();

            var agents = await repositoryClient.GetFromJsonAsync<RepoAgent[]>("fmp.repository/agents/manifest.json");
            foreach (var agent in agents)
            {
                var item = new MenuDataItem();
                item.Path = "";
                item.Name = string.Format("{0}.{1}", agent.Org, agent.Name);
                item.Key = string.Format("{0}.{1}", agent.Org, agent.Name).ToLower();
                item.Icon = "menu";
                int pagesCount = agent.Pages?.Length ?? 0;
                item.Children = new MenuDataItem[pagesCount];
                for (int i = 0; i < pagesCount; i++)
                {
                    item.Children[i] = new MenuDataItem
                    {
                        Path = string.Format("/{0}/{1}/{2}", agent.Org.ToLower(), agent.Name.ToLower(), agent.Pages[i].ToLower()),
                        Name = agent.Pages[i],
                        Key = string.Format("/{0}/{1}/{2}", agent.Org.ToLower(), agent.Name.ToLower(), agent.Pages[i].ToLower()),
                    };
                }
                menuDataItems.Add(item);

                var module = new ModuleRouter.Module();
                module.org = agent.Org;
                module.name = agent.Name;
                module.version = agent.Version;
                module.grpc = string.Format("{0}:{1}", settings.Grpc, agent.Port);
                module.pages.AddRange(agent.Pages ?? new string[0]);
                moduleS.Add(module);
            }
            menuConfig = menuDataItems.ToArray();
            moduleConfig = new ModuleRouter.ModuleConfig();
            moduleConfig.modules = moduleS.ToArray();
        }

        private void applyProSetting(ProSettings _settings)
        {
            proSettings.NavTheme = _settings.NavTheme;
            proSettings.HeaderHeight = _settings.HeaderHeight;
            proSettings.Layout = _settings.Layout;
            proSettings.ContentWidth = _settings.ContentWidth;
            proSettings.Title = _settings.Title;
            proSettings.IconfontUrl = _settings.IconfontUrl;
            proSettings.PrimaryColor = _settings.PrimaryColor;
            proSettings.FixedHeader = _settings.FixedHeader;
            proSettings.FixSiderbar = _settings.FixSiderbar;
            proSettings.ColorWeak = _settings.ColorWeak;
            proSettings.SplitMenus = _settings.SplitMenus;
            proSettings.HeaderRender = _settings.HeaderRender;
            proSettings.FooterRender = _settings.FooterRender;
            proSettings.MenuRender = _settings.MenuRender;
            proSettings.MenuHeaderRender = _settings.MenuHeaderRender;

        }
    }
}

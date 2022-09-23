using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace XTC.FMP.APP.Blazor
{
    public class RuntimeScalingSettings
    {
        public bool Active { get; set; }
        public string Environment { get; set; }
        public string Repository { get; set; }
        public string Grpc { get; set; }
    }

    /// <summary>
    /// 运行时伸缩
    /// </summary>
    public class RuntimeScalingManager
    {
        public HttpClient internalClient { get; private set; }
        public HttpClient repositoryClient { get; private set; }
        public RuntimeScalingSettings settings { get; set; }


        private RepoAgent[] agents_;

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

        public async Task<RepoAgent[]> FetchAgents()
        {
            if (null != agents_)
                return agents_;
            agents_ = await repositoryClient.GetFromJsonAsync<RepoAgent[]>("fmp.repository/agents/manifest.json");
            return agents_;
        }
    }
}

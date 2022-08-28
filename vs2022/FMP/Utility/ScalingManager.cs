using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace XTC.FMP.APP.Blazor
{
    public class ScalingSettings
    {
        public bool Active { get; set; }
        public string Environment { get; set; }
        public string Repository { get; set; }
        public string Grpc { get; set; }
    }

    public class ScalingManager
    {
        public HttpClient internalClient { get; private set; }
        public HttpClient repositoryClient { get; private set; }
        public ScalingSettings settings { get; set; }

        // 使用一个虚拟的协议，调用grpc的通信，以加载相关的库文件
        // ！！！ 缺少这个步骤，在发布为wasm后，动态加载库文件使用grpc通信会崩溃
        public GrpcChannel dummyChannel { get; private set; }

        private RepoAgent[] agents_;

        public ScalingManager()
        {
            dummyChannel = GrpcChannel.ForAddress("https://dummy", new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });
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

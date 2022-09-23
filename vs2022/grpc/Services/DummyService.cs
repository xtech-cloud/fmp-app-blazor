using grpc;
using Grpc.Core;
using XTC.FMP.APP.Blazor.Proto;

namespace grpc.Services
{
    public class DummyService : Dummy.DummyBase
    {
        private readonly ILogger<DummyService> _logger;
        public DummyService(ILogger<DummyService> logger)
        {
            _logger = logger;
        }

        public override Task<CallResponse> Call(CallRequest _request, ServerCallContext context)
        {
            return Task.FromResult(new CallResponse
            {
                StringValue = _request.StringValue,
            });
        }
    }
}
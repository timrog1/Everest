using System;
using System.Net.Http;
using System.Threading.Tasks;
using Everest.Content;
using Everest.Pipeline;

namespace Everest
{
    public interface Resource
    {
        Uri Url { get; }
        Task<Response> Send(HttpMethod method, string uri, BodyContent body, params PipelineOption[] overridingPipelineOptions);
        Resource With(params PipelineOption[] options);
    }
}
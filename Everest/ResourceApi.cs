using System.Net.Http;
using System.Threading.Tasks;
using Everest.Content;
using Everest.Pipeline;

namespace Everest
{
    public static class ResourceApi
    {
        public static Task<Response> Get(this Resource resource, string uri, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Get, uri, null, pipelineOptions);
        }

        public static Task<Response> Options(this Resource resource, string url, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Options, url, null, pipelineOptions);
        }

        public static Task<Response> Post(this Resource resource, string url, BodyContent body,
                                          params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Post, url, body, pipelineOptions);
        }

        public static Task<Response> Post(this Resource resource, string url, string body,
                                          params PipelineOption[] pipelineOptions)
        {
            return resource.Post(url, new StringBodyContent(body), pipelineOptions);
        }

        public static Task<Response> Post(this Resource resource, string body, params PipelineOption[] pipelineOptions)
        {
            return resource.Post(string.Empty, new StringBodyContent(body), pipelineOptions);
        }

        public static Task<Response> Put(this Resource resource, string uri, string body,
                                         params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Put, uri, new StringBodyContent(body), pipelineOptions);
        }

        public static Task<Response> Put(this Resource resource, string uri, BodyContent body,
                                         params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Put, uri, body, pipelineOptions);
        }

        public static Task<Response> Head(this Resource resource, string uri, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Head, uri, null, pipelineOptions);
        }

        public static Task<Response> Delete(this Resource resource, string uri, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Delete, uri, null, pipelineOptions);
        }
    }

    public static class ResourceApiAsync
    {
        public static Task<Response> Get(this Task<Response> resource, string uri, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Get, uri, null, pipelineOptions);
        }

        public static Task<Response> Options(this Task<Response> resource, string url, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Options, url, null, pipelineOptions);
        }

        public static Task<Response> Post(this Task<Response> resource, string url, BodyContent body, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Post, url, body, pipelineOptions);
        }

        public static Task<Response> Post(this Task<Response> resource, string url, string body, params PipelineOption[] pipelineOptions)
        {
            return resource.Post(url, new StringBodyContent(body), pipelineOptions);
        }

        public static Task<Response> Post(this Task<Response> resource, string body, params PipelineOption[] pipelineOptions)
        {
            return resource.Post(string.Empty, new StringBodyContent(body), pipelineOptions);
        }

        public static Task<Response> Put(this Task<Response> resource, string uri, string body, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Put, uri, new StringBodyContent(body), pipelineOptions);
        }

        public static Task<Response> Put(this Task<Response> resource, string uri, BodyContent body, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Put, uri, body, pipelineOptions);
        }

        public static Task<Response> Head(this Task<Response> resource, string uri, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Head, uri, null, pipelineOptions); 
        }

        public static Task<Response> Delete(this Task<Response> resource, string uri, params PipelineOption[] pipelineOptions)
        {
            return resource.Send(HttpMethod.Delete, uri, null, pipelineOptions);
        }

        public async static Task<Response> Send(this Task<Response> resource, HttpMethod method, string uri, BodyContent body, params PipelineOption[] pipelineOptions)
        {
            return await (await resource).Send(method, uri, body, pipelineOptions);
        }

        public async static Task<Resource> With(this Task<Resource> resource, params PipelineOption[] pipelineOptions)
        {
            return (await resource).With(pipelineOptions);
        }

        public async static Task<string> GetBody(this Task<Response> resource)
        {
            return await (await resource).GetBodyAsync();
        }

        public async static Task<byte[]> GetBodyAsByteArray(this Task<Response> resource)
        {
            return await (await resource).GetBodyAsBytesAsync();
        }
    }
}

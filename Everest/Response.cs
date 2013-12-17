using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Everest.Content;
using Everest.Pipeline;

namespace Everest
{
    public interface Response : Resource, IDisposable
    {
        Task<string> GetBodyAsync();
        Task<byte[]> GetBodyAsBytesAsync();
        string ContentType { get; }
        HttpStatusCode StatusCode { get; }
        DateTimeOffset? LastModified { get; }
        string Location { get; }
        IDictionary<string, string> Headers { get; }
    }
}
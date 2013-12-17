using System;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using Everest.Caching;
using Everest.Headers;
using Everest.SystemNetHttp;
using NUnit.Framework;

namespace Everest.UnitTests
{
    [TestFixture]
    public class CachingTests
    {
        private CachingServer _server;
        private RestClient _client;

        [SetUp]
        public void SetUp()
        {
            _server = new CachingServer(12345);
            _client = new RestClient(_server.Url, new CachePolicy { Cache = true });
        }

        [TearDown]
        public void TearDown()
        {
            _server.Stop();
        }

        [Test]
        public async Task ShouldGetCachedResponsesWhenCacheHasNotExpired()
        {
            _server.CacheControl = "public, must-revalidate, max-age=10";
            await ShouldGetFreshResource("request 1");
            await ShouldGetCachedResource("request 1");
        }

        [Test]
        public async Task ShouldGetFreshResponsesWhenCacheHasExpired()
        {
            _server.CacheControl = "public, must-revalidate, max-age=1";
            await ShouldGetFreshResource("request 1");
            await Wait(1);
            await ShouldGetFreshResource("request 2");
        }

        private Task Wait(int seconds)
        {
            return Task.Delay(seconds*1000);
        }

        private async Task ShouldGetCachedResource(string body)
        {
            var numberOfRequests = _server.NumberOfRequests;
            var response = await _client.Get("");
            Assert.That(_server.NumberOfRequests, Is.EqualTo(numberOfRequests));
            Assert.That(await response.GetBodyAsync(), Is.EqualTo(body));
        }

        private async Task ShouldGetFreshResource(string body)
        {
            var numberOfRequests = _server.NumberOfRequests;
            var response = _client.Get("");
            Assert.That(await response.GetBody(), Is.EqualTo(body));
            Assert.That(_server.NumberOfRequests, Is.EqualTo(numberOfRequests + 1));
        }
    }
}
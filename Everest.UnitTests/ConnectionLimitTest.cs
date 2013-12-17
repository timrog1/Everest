using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SelfishHttp;

namespace Everest.UnitTests
{
    [TestFixture]
    public class ConnectionLimitTest
    {
        private const string BaseAddress = "http://localhost:18754";
        private Server _server;

        [SetUp]
        public void StartServer()
        {
            _server = new Server(18754);
            _server.OnGet("/accept").Respond((req, res) => res.Body = "Body");
            _server.OnGet("/sleep").Respond(SleepForASecond);
        }

        [TearDown]
        public void StopServer()
        {
            _server.Stop();
        }

        [Test]
        public async Task ManyConnectionsDoesNotThrowHttpRequestException()
        {
            var client = new RestClient(BaseAddress);
            Assert.DoesNotThrow(async () =>
            {
                // The max number of allowed outbound http requests on my windows appears to be 16336...
                for (var i = 0; i < 17000; i++)
                {
                    using (var response = await client.Get("/accept"))
                    {
                        Assert.AreEqual(await response.GetBodyAsync(), "Body");
                    }
                }
            });
        }

        [Test]
        public async Task ManyConnectionsAreUsedInParallel()
        {
            var client = new RestClient(BaseAddress);
            await SleepOnce(client); // warm up
            var startedAt = DateTime.Now;
            var tasks = Enumerable.Range(1, 50).Select(i => SleepOnce(client)).ToArray();
            await Task.WhenAll(tasks);
            var elapsed = DateTime.Now - startedAt;
            Assert.That(elapsed.TotalSeconds, Is.LessThan(10));
            Console.WriteLine(elapsed.TotalSeconds);
        }

        private static async Task SleepOnce(Resource client)
        {
            using (var response = await client.Get("/sleep"))
            {
                Assert.AreEqual(await response.GetBodyAsync(), "zzz!");
            } 
        }

        private static int _counter;

        private static void SleepForASecond(IRequest req, IResponse res)
        {
            Interlocked.Increment(ref _counter);
            var c = _counter;
            Console.WriteLine("starting request " + c);
            Thread.Sleep(1000);
            res.Body = "zzz!";
            Console.WriteLine("finishing request " + c);
        }
    }
}

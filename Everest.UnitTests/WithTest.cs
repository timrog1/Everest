﻿using System.Threading.Tasks;
using Everest.Headers;
using NUnit.Framework;
using SelfishHttp;

namespace Everest.UnitTests
{
    [TestFixture]
    public class WithTest
    {
        private const string BaseAddress = "http://localhost:18749";
        private Server _server;

        [SetUp]
        public void StartServer()
        {
            _server = new Server(18749);
        }

        [TearDown]
        public void StopServer()
        {
            _server.Stop();
        }

        [Test]
        public async Task AddsOptions()
        {
            _server.OnGet("/headers").Respond((req, res) => res.Body = req.Headers["a"] + "," + req.Headers["b"]);

            var client = new RestClient(BaseAddress);
            var response = await client.With(new RequestHeader("a", "1"), new RequestHeader("b", "2")).Get(BaseAddress + "/headers");
            Assert.That(await response.GetBodyAsync(), Is.EqualTo("1,2"));
            response = await response.With(new RequestHeader("a", "3")).Get("/headers");
            Assert.That(await response.GetBodyAsync(), Is.EqualTo("3,2"));
            response = await response.With(new RequestHeader("b", "4")).Get("/headers");
            Assert.That(await response.GetBodyAsync(), Is.EqualTo("3,4"));
        }
    }
}

﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using SelfishHttp;

namespace Everest.UnitTests
{
    [TestFixture, Ignore("WIP: This test showed random results, don't know why yet")]
    public class StressTest
    {
        private const string BaseAddress = "http://localhost:18749";
        private Server _server;
        private Stopwatch _stopwatch;
        private Uri _requestUri;
        const int Count = 5000;

        [SetUp]
        public void StartServerAndCreateClient()
        {
            _server = new Server(18749);
            _server.OnGet("/foo").RespondWith("foo!");
            _stopwatch = new Stopwatch();
            _requestUri = new Uri(BaseAddress + "/foo");
        }

        [Test]
        public void LotsOfRequests()
        {
            var httpClient = new HttpClient();
            
            Measure("Warm up!", () => MakeRequest(httpClient, _requestUri));

            var reusedClient = new RestClient(BaseAddress);

            Measure("Reused HttpClient", () => MakeRequest(httpClient, _requestUri) );

            Measure("Reused RestClient", () => reusedClient.Get("/foo"));

            Measure("HttpClient per request", () => MakeRequest(new HttpClient(), _requestUri));

            Measure("RestClient per request", () => new RestClient(BaseAddress).Get("/foo"));
        }

        private static async Task<string> MakeRequest(HttpClient httpClient, Uri requestUri)
        {
            var result = await httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = requestUri
            });

            return result.ToString();
        }

        private void Measure(string clientName, Func<Task> action)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            var tasks = Enumerable.Range(1, Count).Select(_ => action()).ToArray();
            Task.WaitAll(tasks);
            _stopwatch.Stop();
            Console.WriteLine(clientName + ": " + _stopwatch.ElapsedMilliseconds);
        }
    }
}

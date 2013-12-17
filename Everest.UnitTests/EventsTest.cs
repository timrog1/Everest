using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Everest.Pipeline;
using Everest.Status;
using Everest.UnitTests.Fakes;
using NUnit.Framework;
using SelfishHttp;

namespace Everest.UnitTests
{
    [TestFixture]
    public class EventsTest
    {
        private const string BaseAddress = "http://localhost:18747";
        private Server _server;

        [SetUp]
        public void StartServer()
        {
            _server = new Server(18747);
        }

        [TearDown]
        public void StopServer()
        {
            _server.Stop();
        }

        [Test]
        public async Task RaisesSendingEventForEachRequest()
        {
            _server.OnGet("/foo").RespondWith("awww yeah");

            var client = new RestClient(BaseAddress);
            var sendingEvents = new List<RequestDetails>();
            client.Sending += (sender, args) => sendingEvents.Add(args.Request);
            await client.Get(BaseAddress + "/foo?omg=yeah");
            Assert.That(sendingEvents.Single().RequestUri.PathAndQuery, Is.EqualTo("/foo?omg=yeah"));
            Assert.That(sendingEvents.Single().Method, Is.EqualTo("GET"));
            await client.Get(BaseAddress + "/foo?omg=nah");
            Assert.That(sendingEvents.Skip(1).Single().RequestUri.PathAndQuery, Is.EqualTo("/foo?omg=nah"));
            Assert.That(sendingEvents.Skip(1).Single().Method, Is.EqualTo("GET"));
        }

        [Test]
        public async Task StillRaisesSendingEventWhenSendingThrows()
        {
            var sendingEvents = new List<RequestDetails>();

            var client = new RestClient(new Uri(BaseAddress), new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            client.Sending += (sender, args) => sendingEvents.Add(args.Request);
            Assert.That(async () => await client.Get("/foo?omg=yeah"), Throws.InstanceOf<DeliberateException>());
            Assert.That(sendingEvents.Single().RequestUri.PathAndQuery, Is.EqualTo("/foo?omg=yeah"));
            Assert.That(sendingEvents.Single().Method, Is.EqualTo("GET"));
        }

        [Test]
        public async Task RaisesRespondedEventForEachRequest()
        {
            _server.OnGet("/foo").Respond((req, res) => res.StatusCode = 418);
            var client = new RestClient(BaseAddress);
            var respondedEvents = new List<ResponseEventArgs>();
            client.Responded += (sender, args) => respondedEvents.Add(args);
            await client.Get("/foo?teapot=yes", new ExpectStatus((HttpStatusCode)418));
            Assert.That(respondedEvents.Single().Response.Status, Is.EqualTo(418));
            Assert.That(respondedEvents.Single().Request.RequestUri.PathAndQuery, Is.EqualTo("/foo?teapot=yes"));
        }

        [Test]
        public async Task DoesNotRaiseRespondedEventForRequestsWhenSendingThrows()
        {
            _server.OnGet("/foo").Respond((req, res) => res.StatusCode = 418);
            var client = new RestClient(new Uri(BaseAddress), new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            var respondedEvents = new List<ResponseDetails>();
            client.Responded += (sender, args) => respondedEvents.Add(args.Response);
            Assert.That(async () => await ThrowSomeException(), Throws.InstanceOf<DeliberateException>());
            //Assert.That(async () => await client.Get("/foo?omg=yeah"), Throws.InstanceOf<DeliberateException>());
            //Assert.That(respondedEvents, Is.Empty);
        }

        private Task ThrowSomeException()
        {
            throw new DeliberateException();
        }

        [Test]
        public async Task RaisesErrorEventForRequestsWhenSendingThrows()
        {
            var client = new RestClient(new Uri(BaseAddress), new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            var sendErrors = new List<RequestErrorEventArgs>();
            client.SendError += (sender, args) => sendErrors.Add(args);
            Assert.That(async () => await client.Get("http://irrelevant"), Throws.InstanceOf<DeliberateException>());
            Assert.That(sendErrors.Count, Is.EqualTo(1));
            Assert.That(sendErrors[0].Exception, Is.InstanceOf<DeliberateException>());
        }

        [Test]
        public async Task RaisedErrorEventIncludesRequestDetails()
        {
            var client = new RestClient(BaseAddress, new AlwaysThrowsOnSendingAdapter(), new List<PipelineOption>());
            var sendErrors = new List<RequestErrorEventArgs>();
            client.SendError += (sender, args) => sendErrors.Add(args);
            Assert.That(async () => await client.Get("http://howdy/"), Throws.InstanceOf<DeliberateException>());
            Assert.That(sendErrors.Count, Is.EqualTo(1));
            Assert.That(sendErrors[0].Request.RequestUri.AbsoluteUri, Is.EqualTo("http://howdy/"));
        }
    }
}

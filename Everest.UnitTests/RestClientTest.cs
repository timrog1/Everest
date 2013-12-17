using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Everest.Content;
using Everest.Headers;
using Everest.Pipeline;
using Everest.Redirection;
using Everest.Status;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SelfishHttp;

namespace Everest.UnitTests
{
    internal static class AsyncThrows
    {
        public static TypeConstraint InstanceOf<T>() where T : Exception
        {
            return InstanceOf(typeof (T));
        }

        public static TypeConstraint InstanceOf(Type type)
        {
            return new AsyncConstraint(type);
        }

        private class AsyncConstraint : InstanceOfTypeConstraint
        {
            public AsyncConstraint(Type type) : base(type)
            {
            }

            public override bool Matches(object actual)
            {

                return base.Matches(actual);
            }
        }
    }

    [TestFixture]
    public class RestClientTest
    {
        private const string BaseAddress = "http://localhost:18745";
        private Server _server;
        private RestClient _client;

        [SetUp]
        public void StartServerAndCreateClient()
        {
            _server = new Server(18745);
            _server.OnGet("/foo").RespondWith("foo!");
            _server.OnGet("/foo/bar").RespondWith("foo bar?");
            _client = new RestClient(BaseAddress);
        }

        [TearDown]
        public void TearDown()
        {
            _server.Stop();
        }

        [Test]
        public async Task ReturnsNewResourceAfterEachRequest()
        {
            var fooResource = await _client.Get("/foo");
            var fooBody = await fooResource.GetBodyAsync();
            Assert.That(fooBody, Is.EqualTo("foo!"));

            var barResource = await fooResource.Get("foo/bar");
            var barBody = await barResource.GetBodyAsync();
            Assert.That(barBody, Is.EqualTo("foo bar?"));
        }

        [Test]
        public async Task FollowsLinksRelativeToResourceEvenAfterRedirect()
        {
            _server.OnGet("/redirect").RedirectTo("/foo/");
            _server.OnGet("/foo/").RespondWith("foo!");
            _server.OnGet("/foo/bar/baz").RespondWith("baz!");
            var body = await _client.Get("/redirect").Get("bar/baz").GetBody();
            Assert.That(body, Is.EqualTo("baz!"));
        }

        [Test]
        public async Task AppliesAmbientOptionsToRedirects()
        {
            _server.OnGet("/redirect").RedirectTo("/x");
            _server.OnGet("/x").Respond((req, res) => res.Body = req.Headers["x-foo"]);
            var client = new RestClient(BaseAddress, new RequestHeader("x-foo", "yippee"));
            var body = await client.Get("/redirect").GetBody();
            Assert.That(body, Is.EqualTo("yippee"));
        }

        [Test]
        public async Task AppliesAuthorizationHeaderToRedirects()
        {
            _server.OnGet("/redirect").RedirectTo("/x");
            _server.OnGet("/x").Respond((req, res) => res.Body = req.Headers["Authorization"]);
            var body = await _client.Get("/redirect", new RequestHeader("Authorization", "yikes"),
                new AutoRedirect { EnableAutomaticRedirection = true, ForwardAuthorizationHeader = true }).GetBody();
            Assert.That(body, Is.EqualTo("yikes"));
        }

        [Test]
        public async Task AppliesAuthorizationHeaderToPermanentRedirects()
        {
            _server.OnGet("/redirect").Respond((req, res) => { res.StatusCode = (int)HttpStatusCode.MovedPermanently;
                res.Headers["Location"] = "/x";
            });
            _server.OnGet("/x").Respond((req, res) => res.Body = req.Headers["Authorization"]);
            var body = await _client.Get("/redirect", new RequestHeader("Authorization", "crumbs"),
                new AutoRedirect { EnableAutomaticRedirection = true, ForwardAuthorizationHeader = true }).GetBody();
            Assert.That(body, Is.EqualTo("crumbs"));
        }

        [Test]
        public async Task AllowsAutoRedirectToBeDisabledForSingleRequest()
        {
            _server.OnGet("/redirect").RedirectTo("/x");
            var response = await _client.Get("/redirect", AutoRedirect.DoNotAutoRedirect);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        }

        [Test]
        public async Task AppliesOverridingOptionsToRedirects()
        {
            _server.OnGet("/redirect").RedirectTo("/x");
            _server.OnGet("/x").Respond((req, res) => res.Body = req.Headers["x-foo"]);
            var body = await _client.Get("/redirect", new RequestHeader("x-foo", "yippee")).GetBody();
            Assert.That(body, Is.EqualTo("yippee"));
        }

        [Test]
        public async Task MakesPutRequests()
        {
            _server.OnPut("/foo").RespondWith(requestBody => "putted " + requestBody);
            var body = await _client.Put("/foo", "body").GetBody();
            Assert.That(body, Is.EqualTo("putted body"));
        }

        [Test]
        public async Task ExposesResponseHeaders()
        {
            _server.OnGet("/whaa").Respond((req, res) => { res.Headers["X-Custom"] = "my custom header"; });

            var response = await _client.Get("/whaa", ExpectStatus.OK);
            Assert.That(response.Headers["X-Custom"], Is.EqualTo("my custom header"));
        }

        [Test]
        public async Task ExposesContentHeadersInTheSameCollectionAsOtherResponseHeaders()
        {
            _server.OnGet("/contentType").Respond((req, res) => { res.Headers["Content-Type"] = "x/foo"; });

            var response = await _client.Get("/contentType");
            Assert.That(response.Headers.ContainsKey("Content-Type"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("x/foo"));
        }

        [Test]
        public async Task MakesOptionsRequests()
        {
            _server.OnOptions("/whaa").RespondWith("options!");
            var body = await _client.Options("/whaa", ExpectStatus.OK).GetBody();
            Assert.That(body, Is.EqualTo("options!"));
        }

        [Test]
        public async Task MakesPostRequests()
        {
            _server.OnPost("/foo").RespondWith(requestBody => "posted " + requestBody);
            var body = await _client.Post("/foo", "body", ExpectStatus.OK).GetBody();
            Assert.That(body, Is.EqualTo("posted body"));
        }

        [Test]
        public async Task MakesPostRequestsWithBodyContent()
        {
            _server.OnPost("/foo").RespondWith(requestBody => "posted " + requestBody);
            var body = await _client.Post("/foo", new StringBodyContent("body"), ExpectStatus.OK).GetBody();
            Assert.That(body, Is.EqualTo("posted body"));
        }

        [Test]
        public async Task MakesHeadRequests()
        {
            _server.OnHead("/foo").Respond((req, res) => res.StatusCode = 303);
            var response = await _client.Head("/foo", new ExpectStatus(HttpStatusCode.SeeOther));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.SeeOther));
        }

        [Test]
        public async Task MakesPostRequestsToSelf()
        {
            _server.OnGet("/self").RespondWith("good on ya");
            _server.OnPost("/self").RespondWith(requestBody => "posted " + requestBody);
            var response = await _client.Get("/self");
            var body = await response.Post("body").GetBody();
            Assert.That(body, Is.EqualTo("posted body"));
        }

        [Test]
        public async Task CanPutBinaryContentInStream()
        {
            _server.OnPut("/image").Respond((req, res) =>
                {
                    var body = req.BodyAs<Stream>();
                    res.Headers["Content-Type"] = req.Headers["Content-Type"];
                    res.Body = body;
                });

            var image = await _client.Put("/image", new StreamBodyContent(new MemoryStream(Encoding.UTF8.GetBytes("this is an image")), "image/png"));
            Assert.That(image.ContentType, Is.EqualTo("image/png"));
            Assert.That(await image.GetBodyAsync(), Is.EqualTo("this is an image"));
        }

        [Test]
        public async Task GetExpects200RangeStatusByDefault()
        {
            try
            {
                await _client.Get("/non-existent");
            }
            catch (UnexpectedStatusException e)
            {
                Assert.That(e.Message, Is.EqualTo("GET http://localhost:18745/non-existent -- expected response status to be not in the range 400-599, got 404 (NotFound)\n\n\n"));
            }
        }

        [Test]
        public async Task GetExpectStatusIsOverridable()
        {
            Assert.That(async () => await _client.Get("/foo", new ExpectStatus(HttpStatusCode.InternalServerError)), 
                Throws.InstanceOf<UnexpectedStatusException>());
        }

        [Test]
        public async Task PutExpectStatusIsOverridable()
        {
            Assert.That(async () => 
                await _client.Put("/foo", "oops", new ExpectStatus(HttpStatusCode.InternalServerError)), Throws.InstanceOf<UnexpectedStatusException>());
        }

        [Test]
        public async Task ThrowsWhenUnsupportedPerRequestOptionsAreSupplied()
        {
            Assert.That(async () => await _client.Get("/foo", new BogusOption()), Throws.InstanceOf<UnsupportedOptionException>());
        }

        [Test]
        public async Task ThrowsWhenUnsupportedAmbientOptionsAreSupplied()
        {
            _server.OnGet("/blah").RespondWith("ok!");
            Assert.That(async () => await new RestClient(BaseAddress, new BogusOption()).Get("/blah"), Throws.InstanceOf<UnsupportedOptionException>());
        }

        [Test]
        public async Task AppliesPipelineOptionsToSubsequentRequests()
        {
            _server.OnGet("/headers").Respond((req, res) => res.Body = req.Headers["x-per-client"]);

            var client = new RestClient(BaseAddress, new SetRequestHeaders(new Dictionary<string, string> { { "x-per-client", "x" } }));
            var firstResponse = client.Get("/headers");
            Assert.That(await firstResponse.GetBody(), Is.EqualTo("x"));
            var secondResponse = firstResponse.Get("/headers");
            Assert.That(await secondResponse.GetBody(), Is.EqualTo("x"));
        }

        [Test]
        public async Task AppliesPipelineOptionsPerRequest()
        {
            _server.OnGet("/headers").Respond((req, res) => res.Body = req.Headers["x-per-client"]);

            var client = new RestClient(BaseAddress);
            var firstResponse = await client.Get("/headers", new SetRequestHeaders(new Dictionary<string, string> { { "x-per-client", "x" } }));
            Assert.That(await firstResponse.GetBodyAsync(), Is.EqualTo("x"));
            var secondResponse = firstResponse.Get("/headers");
            Assert.That(await secondResponse.GetBody(), Is.EqualTo(""));
        }

        [Test]
        public async Task ProvidesAConvenientWayToSetAcceptHeader()
        {
            _server.OnGet("/accept").Respond((req, res) => res.Body = req.Headers["Accept"]);
            var client = new RestClient(BaseAddress);
            var response = await client.Get("/accept", new Accept("foo/bar"));
            Assert.That(await response.GetBodyAsync(), Is.EqualTo("foo/bar"));
        }

        [Test]
        public async Task CanComputeHeadersDynamically()
        {
            var i = 0;
            var dynamicRequestHeaders = new DynamicRequestHeaders(
                () => new Dictionary<string, string> { { "X", (++i).ToString(CultureInfo.InvariantCulture) } });

            _server.OnGet("/X").Respond((req, res) => res.Body = req.Headers["X"]);
            var client = new RestClient(BaseAddress, new SetRequestHeaders(dynamicRequestHeaders));
 
            Assert.That(await client.Get("/X").GetBody(), Is.EqualTo("1"));
            Assert.That(await client.Get("/X").GetBody(), Is.EqualTo("2"));
        }

        class DynamicRequestHeaders : IEnumerable<KeyValuePair<string, string>>
        {
            private readonly Func<Dictionary<string, string>> _func;

            public DynamicRequestHeaders(Func<Dictionary<string, string>> func)
            {
                _func = func;
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return _func().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Test]
        public async Task ThrowsWhenExpectedResponseHeaderIsNotSet()
        {
            _server.OnGet("/respond-with-bar").RespondWith("oops, no x header!");
            var client = new RestClient(BaseAddress, new ExpectResponseHeaders { { "x", "foo" }});
            try
            {
                await client.Get("/respond-with-bar");
                Assert.Fail("Expected UnexpectedResponseHeaderException");
            }
            catch (UnexpectedResponseHeaderException e)
            {
                Assert.That(e.Key, Is.EqualTo("x"));
                Assert.That(e.ExpectedValue, Is.EqualTo("foo"));
                Assert.That(e.ActualValue, Is.EqualTo(null));
                Assert.That(e.Message, Is.EqualTo("Expected response header 'x' to have the value 'foo', but it had the value ''"));
            }
        }

        [Test]
        public async Task ThrowsWhenExpectedResponseHeaderHasUnexpectedValue()
        {
            _server.OnGet("/respond-with-bar").Respond((req, res) => res.Headers["x"] = "bar");
            var client = new RestClient(BaseAddress, new ExpectResponseHeaders { { "x", "foo" }});
            try
            {
                await client.Get("/respond-with-bar");
                Assert.Fail("Expected UnexpectedResponseHeaderException");
            }
            catch (UnexpectedResponseHeaderException e)
            {
                Assert.That(e.Key, Is.EqualTo("x"));
                Assert.That(e.ExpectedValue, Is.EqualTo("foo"));
                Assert.That(e.ActualValue, Is.EqualTo("bar"));
                Assert.That(e.Message, Is.EqualTo("Expected response header 'x' to have the value 'foo', but it had the value 'bar'"));
            }
        }

        [Test]
        public async Task DoesNotThrowWhenExpectedResponseContentHeadersAreSetToExpectedValues()
        {
            _server.OnGet("/respond-with-foo").Respond((req, res) => res.Headers["Content-Type"] = "x/foo");
            var client = new RestClient(BaseAddress, new ExpectResponseHeaders { { "Content-Type", "x/foo" } });
            Assert.That(async () => await client.Get("/respond-with-foo"), Throws.Nothing);
        }

        [Test]
        public async Task ThrowsWhenExpectedResponseContentHeaderIsNotSet()
        {
            _server.OnGet("/respond-with-bar").Respond((req, res) => res.Headers["Content-Type"] = null);
            var client = new RestClient(BaseAddress, new ExpectResponseHeaders { { "Content-Type", "oh/really" } });
            try
            {
                await client.Get("/respond-with-bar");
                Assert.Fail("Expected UnexpectedResponseHeaderException");
            }
            catch (UnexpectedResponseHeaderException e)
            {
                Assert.That(e.Key, Is.EqualTo("Content-Type"));
                Assert.That(e.ExpectedValue, Is.EqualTo("oh/really"));
                Assert.That(e.ActualValue, Is.EqualTo(null));
                Assert.That(e.Message, Is.EqualTo("Expected response header 'Content-Type' to have the value 'oh/really', but it had the value ''"));
            }
        }

        [Test]
        public async Task ThrowsWhenExpectedResponseContentHeaderHasUnexpectedValue()
        {
            _server.OnGet("/respond-with-bar").Respond((req, res) => res.Headers["Content-Type"] = "x/bar");
            var client = new RestClient(BaseAddress, new ExpectResponseHeaders { { "Content-Type", "x/foo" } });
            try
            {
                await client.Get("/respond-with-bar");
                Assert.Fail("Expected UnexpectedResponseHeaderException");
            }
            catch (UnexpectedResponseHeaderException e)
            {
                Assert.That(e.Key, Is.EqualTo("Content-Type"));
                Assert.That(e.ExpectedValue, Is.EqualTo("x/foo"));
                Assert.That(e.ActualValue, Is.EqualTo("x/bar"));
                Assert.That(e.Message, Is.EqualTo("Expected response header 'Content-Type' to have the value 'x/foo', but it had the value 'x/bar'"));
            }
        }

        [Test]
        public async Task DoesNotThrowWhenExpectedResponseHeadersAreSetToExpectedValues()
        {
            _server.OnGet("/respond-with-foo").Respond((req, res) => res.Headers["x"] = "foo");
            var client = new RestClient(BaseAddress, new ExpectResponseHeaders { { "x", "foo" } });
            Assert.That(async () => await client.Get("/respond-with-foo"), Throws.Nothing);
        }

        [Test]
        public void IsInstantiableWithNoArguments()
        {
            Assert.That(new RestClient().Url, Is.EqualTo(null));
        }

        [Test]
        public void IsInstantiableWithStringAsUrl()
        {
            Assert.That(new RestClient("http://www.featurist.co.uk/").Url.AbsoluteUri, Is.EqualTo("http://www.featurist.co.uk/"));
        }

        [Test]
        public async Task AcceptsStarSlashStarByDefault()
        {
            _server.OnGet("/accept").Respond((req, res) => res.Body = req.Headers["Accept"]);
            Assert.That(await new RestClient(BaseAddress).Get("/accept").GetBody(), Is.EqualTo("*/*"));
        }

        [Test]
        public async Task AcceptsGzipAndDeflateEncodingByDefault()
        {
            _server.OnGet("/accept-encoding").Respond((req, res) => { res.Body = req.Headers["Accept-Encoding"]; });
            Assert.That(await _client.Get("/accept-encoding").GetBody(), Is.EqualTo("gzip, deflate"));
        }

        [Test]
        public async Task AddsContentHeadersToContent()
        {
            _server.OnPut("/testput").AddHandler((context, next) =>
                {
                    Assert.That(context.Request.Headers["content-encoding"], Is.EqualTo("gzip"));
                    next();
                });

            var content = new StreamBodyContent(new MemoryStream(Encoding.UTF8.GetBytes("Test")), "text/plain");
            content.Headers.Add("Content-Encoding", "gzip");
            await _client.Put("/testput", content);
        }

        private class BogusOption : PipelineOption
        {
        }
    }
}

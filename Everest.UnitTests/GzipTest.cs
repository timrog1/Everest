using System.Threading.Tasks;
using Everest.Headers;
using NUnit.Framework;

namespace Everest.UnitTests
{
    [TestFixture]
    public class GzipTest
    {
        [Test]
        public async Task ReadsGzippedContent()
        {
            var response = new RestClient().Get("http://httpbin.org/gzip");
            Assert.That(await response.GetBody(), Is.StringContaining("gzipped"));
        }
    }
}

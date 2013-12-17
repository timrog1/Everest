using System.Threading.Tasks;
using System.Xml.Linq;

namespace Everest
{
    public static class XmlExtensions
    {
        public static async Task<XDocument> BodyAsXmlAsync(this Response response)
        {
            return XDocument.Parse(await response.GetBodyAsync());
        }
    }
}
    ;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Negotiator
{
    public class BindProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly XmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public Jid JidForConnectedResource { get; set; }

        public XName FeatureName { get; } = XNames.bind_bind;
        public bool IsNegotiated { get; private set; }

        public BindProtocolNegotiator(XmppStream xmppServerStream, Dictionary<string, string> runtimeParameters)
        {
            this.xmppServerStream = xmppServerStream;
            this.runtimeParameters = runtimeParameters;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            var resource = options["resource"];

            var requestIq = new Iq(IqType.set, new Bind(resource), name: "iq");

            var responseIq = await this.xmppServerStream.WriteIqAndReadReponseAsync(requestIq);

            Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            this.runtimeParameters["jid"] = responseIq.Element(XNames.bind_bind).Element(XNames.bind_jid).Value;

            this.IsNegotiated = true;
            return true;
        }
    }
}
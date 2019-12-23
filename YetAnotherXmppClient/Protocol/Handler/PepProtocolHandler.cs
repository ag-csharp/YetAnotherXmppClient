﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class PepProtocolHandler : ProtocolHandlerBase
    {
        public PepProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
        }

        //XEP-0163/6.1
        public async Task<bool> DetermineSupportAsync()
        {
            //UNDONE use ServiceDiscoveryProtoHandler
            var iq = new Iq(IqType.get, new XElement(XNames.discoinfo_query))
            {
                From = this.RuntimeParameters["jid"],
                To = this.RuntimeParameters["jid"].ToBareJid()
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);

            var pepSupported = iqResp.Element(XNames.discoinfo_query).Elements(XNames.discoinfo_identity)
                .Any(idt => idt.Attribute("category")?.Value == "pubsub" &&
                            idt.Attribute("type")?.Value == "pep");

            return pepSupported;
        }

        public async Task PublishEventAsync(string node, string itemId, XElement content)
        {
            //var nodeId = Guid.NewGuid().ToString();
            //var itemId = (string)null;
            var iq = new Iq(IqType.set, new PubSubPublish(node, itemId, content));
        }

        public async Task SubscribeToNodeAsync(string nodeId)
        {
            var iq = new Iq(IqType.set, new PubSubSubscribe(nodeId, this.RuntimeParameters["jid"].ToBareJid()))
            {
                From = this.RuntimeParameters["jid"],
                To = this.RuntimeParameters["jid"].ToBareJid() //UNDONE only server?
            };

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);
        }
    }
}

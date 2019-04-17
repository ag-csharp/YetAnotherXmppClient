using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Protocol.Negotiator
{
    public class SaslFeatureProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly XmppStream xmppStream;
        private readonly IEnumerable<string> clientMechanisms;

        public XName FeatureName { get; } = XNames.sasl_mechanisms;
        public bool IsNegotiated { get; private set; }

        public SaslFeatureProtocolNegotiator(XmppStream xmppStream/*Stream serverStream*/, IEnumerable<string> clientMechanisms) //: base(serverStream)
        {
            this.xmppStream = xmppStream;
            this.clientMechanisms = clientMechanisms;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            //6.3.3. Mechanism Preferences
            var mechanismToTry = this.clientMechanisms.Intersect(((MechanismsFeature)feature).Mechanisms).FirstOrDefault();

            Log.Debug($"Trying SASL mechanism '{mechanismToTry}'");

            if (mechanismToTry == null)
            {
                throw new InvalidOperationException("no supported sasl mechanism");
            }

            var success = await this.NegotiateInternalAsync(mechanismToTry, options);
            if (success)
                this.IsNegotiated = true;

            return success;

        }

        private async Task<bool> NegotiateInternalAsync(string mechanismToTry, Dictionary<string, string> options)
        {
            var username = options["username"];
            var password = options["password"];
            //6.4.2. Initiation
            await this.WriteInitiationAsync(mechanismToTry, username, password);

            //6.4.3. Challenge-Response Sequence
            XElement xElem;
            while(true)
            {
                //var xmlFragment = await this.xmlReader.ReadElementOrClosingTagAsync();//this.xmlReader.ReadNextElementAsync();
                //Expect(() => xmlFragment.PartType == XmlPartType.Element);
                //xElem = XElement.Parse(xmlFragment.RawXml);
                xElem = await this.xmppStream.ReadElementAsync();
                if (xElem.Name == XNames.sasl_challenge)
                {
                    await this.xmppStream.WriteElementAsync(new XElement(XNames.sasl_response));
                }
                else
                {
                    break;
                }
            }
            
            //6.4.5. SASL Failure
            //6.4.6. SASL Success
            Expectation.Expect(XNames.sasl_success, actual: xElem.Name, context: xElem);

            return true;
        }

        private async Task WriteInitiationAsync(string mechanism, string username, string password)
        {
            var xElem = new XElement(XNames.sasl_auth, new XAttribute(XNames.sasl_mechanism.LocalName, mechanism),
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{(char) 0}{username}{(char) 0}{password}"))
            );

            await this.xmppStream.WriteElementAsync(xElem);
        }
    }
}
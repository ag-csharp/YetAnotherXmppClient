using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace YetAnotherXmppClient
{
    public class Feature
    {
        public XName Name { get; set; }
        public bool IsRequired { get; set; }
    }

    //    public class Features : List<IFeature>
    //    {
    //        
    //    }


    public class MechanismsFeature : Feature
    {
        public IEnumerable<string> Mechanisms { get; set; }

        private MechanismsFeature()
        {
            this.Name = XNames.sasl_mechanisms;
            this.IsRequired = false;
        }
        public static MechanismsFeature FromXElement(XElement xElem)
        {
            return new MechanismsFeature { Mechanisms = xElem.Elements(XNames.sasl_mechanism).Select(xe => xe.Value) };
        }
    }


    public static class Features
    {
        public static IEnumerable<Feature> FromXElement(XElement xElem)
        {
            Expectation.Expect(XNamespaces.stream + "features", xElem.Name, xElem);
            foreach (var featureElem in xElem.Elements())
            {
                if (featureElem.Name == XNames.sasl_mechanisms)
                {
                    yield return MechanismsFeature.FromXElement(featureElem);
                }
                else
                {
                    yield return new Feature { Name = featureElem.Name, IsRequired = featureElem.Elements().Any(sub => sub.Name.LocalName == "required") };
                }
            }
        }
    }
}
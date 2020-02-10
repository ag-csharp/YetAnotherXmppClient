﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    internal class EntitySupportsFeatureQuery : IQuery<bool>
    {
        public string FullJid { get; }
        public string ProtocolNamespace { get; }

        public EntitySupportsFeatureQuery(string fullJid, string protocolNamespace)
        {
            this.FullJid = fullJid;
            this.ProtocolNamespace = protocolNamespace;
        }
    }
}
﻿using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class EntityInformationTreeQuery : IQuery<EntityInfo>
    {
        public string Jid { get; set; } //null: use server jid
    }
}
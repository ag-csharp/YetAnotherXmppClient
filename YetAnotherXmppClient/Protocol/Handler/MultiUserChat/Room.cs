﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core.StanzaParts;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class Occupant
    {
        public string Nickname { get; }
        public string FullJid { get; }
        public Affiliation Affiliation { get; }
        public Role Role { get; }
        public PresenceShow Show { get; internal set; }
        public string Status { get; internal set; }

        internal Occupant(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            this.Nickname = nickname;
            this.FullJid = fullJid;
            this.Affiliation = affiliation;
            this.Role = role;
        }
    }

    public enum OccupantUpdateCause
    {
        Added,
        Changed
    }

    public class Room
    {
        private readonly MultiUserChatProtocolHandler protocolHandler;

        // <nickname, Occupant>
        private readonly ConcurrentDictionary<string, Occupant> occupants = new ConcurrentDictionary<string, Occupant>();
        private string errorText;

        public string Jid { get; }
        public string Name { get; } //UNDONE 

        private string subject;
        public string Subject
        {
            get => this.subject;
            set
            {
                this.subject = value;
                this.SubjectChanged?.Invoke(this, value);
            }
        }

        public RoomType Type { get; internal set; }

        public bool IsLogging { get; internal set; }

        public Occupant Self { get; private set; }
        public IEnumerable<Occupant> Occupants => this.occupants.Values;

        //UNDONE TypeUpdated event?

        public event EventHandler<Occupant> SelfUpdated;
        public event EventHandler<(Occupant Occupant, OccupantUpdateCause Cause)> OccupantsUpdated;

        public event EventHandler<string> SubjectChanged; 

        private event EventHandler<string> errorOccurred;
        public event EventHandler<string> ErrorOccurred
        {
            add
            {
                this.errorOccurred += value;
                if(this.errorText != null)
                    value?.Invoke(this, this.errorText);
            }
            remove => this.errorOccurred -= value;
        }

        internal Room(MultiUserChatProtocolHandler protocolHandler, string jid)
        {
            this.protocolHandler = protocolHandler;
            this.Jid = jid;
        }

        internal void AddOrUpdateOccupant(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            var cause = OccupantUpdateCause.Added;
            var occupant = this.occupants.AddOrUpdate(nickname, _ => new Occupant(nickname, fullJid, affiliation, role), (_, existing) =>
                {
                    cause = OccupantUpdateCause.Changed;
                    return new Occupant(nickname, fullJid, affiliation, role);
                });
            this.OccupantsUpdated?.Invoke(this, (occupant, cause));

            if (fullJid != null) //UNDONE not really needed as advertised with status-code-100?!
            {
                this.Type = RoomType.NonAnonymous;
            }
        }

        internal void SetSelf(string nickname, string fullJid, Affiliation affiliation, Role role)
        {
            this.Self = new Occupant(nickname, fullJid, affiliation, role);
            this.SelfUpdated?.Invoke(this, this.Self);
        }

        internal void UpdateOccupantsShow(string nickname, PresenceShow show)
        {
            if (this.occupants.TryGetValue(nickname, out var occupant))
            {
                occupant.Show = show;
            }
        }

        internal void UpdateOccupantsStatus(string nickname, string status)
        {
            if (this.occupants.TryGetValue(nickname, out var occupant))
            {
                occupant.Status = status;
            }
        }

        internal void OnError(string errorText)
        {
            this.errorText = errorText;
            this.errorOccurred?.Invoke(this, errorText);
        }

        public Task ChangeAvailabilityAsync(PresenceShow show, string status = null)
        {
            return this.protocolHandler.ChangeAvailabilityAsync(this.Jid, show, status);
        }
    }
}
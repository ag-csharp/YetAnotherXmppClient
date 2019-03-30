﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol;

namespace YetAnotherXmppClient.Core
{
    public class XmppStream
    {
        private XmlReader xmlReader;
        private TextWriter textWriter;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<XElement>> iqCompletionSources = new ConcurrentDictionary<string, TaskCompletionSource<XElement>>();
        private readonly ConcurrentDictionary<XNamespace, IServerIqCallback> serverIqCallbacks = new ConcurrentDictionary<XNamespace, IServerIqCallback>();

        public XmppStream(Stream serverStream)
        {
            this.xmlReader = XmlReader.Create(serverStream, new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment, IgnoreWhitespace = true });
            this.textWriter = new DebugTextWriter(new StreamWriter(serverStream));
        }

        internal void RegisterServerIqCallback(XNamespace iqContentNamespace, IServerIqCallback callback)
        {
            serverIqCallbacks.TryAdd(iqContentNamespace, callback);
        }

        public async Task<XElement> WriteIqAndReadReponseAsync(Iq iq)
        {
            Log.Logger.Verbose($"WriteIqAndReadReponseAsync ({iq.Id})");

            var tcs = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            this.iqCompletionSources.TryAdd(iq.Id, tcs);

            await this.textWriter.WriteAndFlushAsync(iq);

            XElement xElem;
            do
            {
                xElem = await this.ReadSingleElementInternalAsync();
                if (xElem.IsIq())
                    this.OnIqReceived(xElem);
                else
                    this.OnOtherElementReceived(xElem);
            } while (!xElem.IsIq() || (xElem.IsIq() && xElem.Attribute("id").Value != iq.Id));


            return await tcs.Task;
        }


        private void OnIqReceived(XElement iqElement)
        {
            if (this.iqCompletionSources.TryRemove(iqElement.Attribute("id").Value, out var tcs))
            {
                Log.Logger.Verbose($"Received iq with awaiter ({iqElement.Attribute("id")?.Value})");
                tcs.SetResult(iqElement);
            }
            else
            {
                if (iqElement.FirstNode is XElement iqContentElem &&
                    this.serverIqCallbacks.TryGetValue(iqContentElem.Name.Namespace, out var callback))
                {
                    callback.IqReceived(iqElement);
                }
                else
                {
                    Log.Logger.Verbose($"Received iq WITHOUT awaiter or callback ({iqElement.Attribute("id")?.Value})");
                }
            }
        }

        private void OnOtherElementReceived(XElement xElem)
        {
            Log.Logger.Error($"Received not expected element which is not handled ({xElem})");

        }

        private async Task<XElement> ReadSingleElementInternalAsync()
        {
            var xElem = await this.xmlReader.ReadNextElementAsync();
            //if (xElem.IsIq())
            //{
            //    this.OnIqReceived(xElem);
            //}

            return xElem;
        }

        public async Task<XElement> ReadNonIqElementAsync()
        {
            XElement xElem;
            do
            {
                xElem = await this.ReadSingleElementInternalAsync();
                if (xElem.IsIq())
                {
                    this.OnIqReceived(xElem);
                }
            } while (xElem.IsIq());

            return xElem;
        }


        public void StartReadLoop()
        {
            Task.Run(async () =>
            {
                try
                {
                    XElement xElem;
                    while (true)
                    {
                        xElem = await this.ReadSingleElementInternalAsync();
                        if (xElem.IsIq())
                            this.OnIqReceived(xElem);
                        else
                            this.OnOtherElementReceived(xElem);
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Error("XmppStream-READLOOP exited: " + e);
                }
            });
        }

        public Task WriteAsync(string message)
        {
            return this.textWriter.WriteAndFlushAsync(message);
        }
    }

    static class XElementExtensions
    {
        public static bool IsIq(this XElement xElem)
        {
            return xElem.Name == "iq";
        }
    }
}

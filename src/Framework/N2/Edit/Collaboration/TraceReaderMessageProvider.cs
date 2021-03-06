﻿using N2.Edit.Collaboration;
using N2.Plugin;
using N2.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace N2.Edit.Collaboration
{
	[MessageSource]
	public class TraceReaderMessageProvider : MessageSourceBase, IAutoStart
	{
		private Queue<CollaborationMessage> messages = new Queue<CollaborationMessage>();
		private TraceEventType reportingLevel;

		public TraceReaderMessageProvider(N2.Configuration.ConfigurationManagerWrapper config)
		{
			reportingLevel = config.Sections.Management.Collaboration.ErrorReportingLevel;
		}

		public override IEnumerable<CollaborationMessage> GetMessages(N2.Edit.Collaboration.CollaborationContext context)
		{
			return messages.Reverse().ToList();
		}

		public void Start()
		{
			Trace.Listeners.Add(new Listener(this));
		}

		public void Stop()
		{
			Trace.Listeners.Remove("TraceReaderMessageProviderListener");
		}

		internal void Add(CollaborationMessage message)
		{
			messages.Enqueue(message);
			if (messages.Count > 10)
				messages.Dequeue();
		}

		class Listener : TraceListener
		{
			private TraceReaderMessageProvider messageProvider;

			public Listener(TraceReaderMessageProvider messageProvider)
			{
				Name = "TraceReaderMessageProviderListener";
				this.messageProvider = messageProvider;
			}

			public override void Write(string message)
			{
			}

			public override void WriteLine(string message)
			{
			}

			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
			{
				if (eventType <= messageProvider.reportingLevel)
					messageProvider.Add(new CollaborationMessage { Title = Split(string.Format(format, args))[0], Text = Split(string.Format(format, args))[1], Alert = false, Updated = eventCache.DateTime, RequiredPermission = Permission.Administer });
				base.TraceEvent(eventCache, source, eventType, id, format, args);
			}

			public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
			{
				if (eventType <= messageProvider.reportingLevel)
					messageProvider.Add(new CollaborationMessage { Title = Split(message)[0], Text = Split(message)[1], Alert = false, Updated = eventCache.DateTime, RequiredPermission = Permission.Administer });
				base.TraceEvent(eventCache, source, eventType, id, message);
			}

			private string[] Split(string message)
			{
				var i = message.IndexOfAny(Environment.NewLine.ToCharArray());
				if (i > 0)
					return new string[] { message.Substring(0, i), message.Substring(i).Trim() };
				else
					return new string[] { message, null };
			}
		}
	}
}
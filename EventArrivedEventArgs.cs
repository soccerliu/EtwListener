
using System;

namespace Samples.Eventing
{
    public sealed class EventArrivedEventArgs : EventArgs
    {
        internal EventArrivedEventArgs(Exception exception)
            : this(Guid.Empty, ushort.MinValue, string.Empty, new PropertyBag())
        {
            this.EventException = exception;
        }

        internal EventArrivedEventArgs(Guid providerId, ushort eventId, string eventName, PropertyBag properties)
        {
            this.EventName = eventName;
            this.EventId = eventId;
            this.ProviderId = providerId;
            this.Properties = properties;
        }

        public Exception EventException
        {
            private set;
            get;
        }

        public string EventName
        {
            private set;
            get;
        }

        public PropertyBag Properties
        {
            private set;
            get;
        }

        public Guid ProviderId
        {
            private set;
            get;
        }

        public ushort EventId
        {
            private set;
            get;
        }

        public uint ProcessId
        {
            private set;
            get;
        }
    }
}
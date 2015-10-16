using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class CustomerOrderActorMessageId : IFormattable, IComparable, IComparable<CustomerOrderActorMessageId>, IEquatable<CustomerOrderActorMessageId>
    {
        [DataMember]
        public ActorId sendingActorId { get; private set; }
        [DataMember]
        public long messageId { get; private set; }

        public CustomerOrderActorMessageId(ActorId sendingActorId, long messageId)
        {
            this.sendingActorId = sendingActorId;
            this.messageId = messageId;
        }

        public static CustomerOrderActorMessageId GetRandom()
        {
            ActorId id = new ActorId(Guid.NewGuid());
            Random r = new Random();
            return new CustomerOrderActorMessageId(id, r.Next());
        }
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("{0}|{1}", sendingActorId.ToString(), messageId);
        }
        public int CompareTo(CustomerOrderActorMessageId other)
        {
            if (this.sendingActorId.ToString().CompareTo(other.sendingActorId.ToString()) > 1)
            {
                return 1;
            }
            else if (this.sendingActorId.ToString().CompareTo(other.sendingActorId.ToString()) < 1)
            {
                return -1;
            }
            else if (this.messageId > other.messageId)
            {
                return 1;
            }
            else if (this.messageId < other.messageId)
            {
                return -1;
            }

            return 0;
        }

        int IComparable.CompareTo(object obj)
        {
            return this.CompareTo((CustomerOrderActorMessageId)obj);
        }

        public bool Equals(CustomerOrderActorMessageId other)
        {
            return (this.sendingActorId.Equals(other.sendingActorId) && this.messageId == other.messageId);
        }
        public static bool operator ==(CustomerOrderActorMessageId item1, CustomerOrderActorMessageId item2)
        {
            return item1.Equals(item2);
        }

        public static bool operator !=(CustomerOrderActorMessageId item1, CustomerOrderActorMessageId item2)
        {
            return !item1.Equals(item2);
        }
        public static bool operator >(CustomerOrderActorMessageId item1, CustomerOrderActorMessageId item2)
        {
            int result = item1.CompareTo(item2);
            return (result == 0 | result == -1);
        }

        public static bool operator <(CustomerOrderActorMessageId item1, CustomerOrderActorMessageId item2)
        {
            int result = item1.CompareTo(item2);
            return (result == 0 | result == 1);
        }

        public override bool Equals(object obj)
        {
            return (this.CompareTo(obj as CustomerOrderActorMessageId) == 0);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }

}

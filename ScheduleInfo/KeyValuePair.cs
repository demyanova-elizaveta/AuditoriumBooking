using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ScheduleInfo
{
    [Serializable]
    [XmlType(TypeName = "SpecifiedEventsColors")]
    public class KeyValuePair<K, V>
    {
        public K Key { get; set; }

        public V Value { get; set; }
        public KeyValuePair() { }

        public KeyValuePair(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }
}

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace FlightEvents
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Airports
    {
        [XmlElement("Airport")]
        public Airport[] Airport { get; set; }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class Airport
    {
        [XmlAttribute]
        public string Ident { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public double Latitude { get; set; }

        [XmlAttribute]
        public double Longitude { get; set; }

        [XmlAttribute]
        public double Elevation { get; set; }

        [XmlAttribute]
        public string Continent { get; set; }

        [XmlAttribute]
        public string Country { get; set; }

        [XmlAttribute]
        public string Region { get; set; }

        [XmlAttribute]
        public string Municipality { get; set; }
    }


}

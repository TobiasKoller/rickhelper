using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace rickhelper
{
	[XmlType("gameList")]
	public class GameList
	{
		[XmlElement("provider")]
		public Provider Provider { get; set; }
		[XmlElement("game")]
		public List<Game> Games { get; set; }
    }

	[XmlType("provider")]
	public class Provider
    {
		[XmlElement("System")]
		public string System { get; set; }
		[XmlElement("software")]
		public string Software { get; set; }
		[XmlElement("database")]
		public string Database { get; set; }
		[XmlElement("web")]
		public string Web { get; set; }

    }
}

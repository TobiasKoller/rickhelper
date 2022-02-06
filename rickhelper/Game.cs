using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace rickhelper
{
    [XmlType("game")]
    public class Game
    {
        [XmlElement("path")]
        public string Path { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("sortname")]
        public string SortName { get; set; }

        [XmlElement("desc")]
        public string Description { get; set; }

        [XmlElement("rating")]
        public string Rating { get; set; }

        [XmlElement("releasedate")]
        public string ReleaseDate { get; set; }

        [XmlElement("developer")]
        public string Developer { get; set; }

        [XmlElement("publisher")]
        public string Publisher { get; set; }

        [XmlElement("genre")]
        public string Genre { get; set; }

        [XmlElement("players")]
        public string Players { get; set; }

        [XmlElement("image")]
        public string Image { get; set; }

        [XmlElement("video")]
        public string Video { get; set; }
        
        public Game()
        {
            SortName = "";
        }
    }
}

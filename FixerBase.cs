using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace rickhelper
{
    public abstract class FixerBase : ITool
    {
        protected Configuration Config;
        protected FixerBase(Configuration configuration)
        {
            Config = configuration;
        }
        public abstract void Run();

        protected GameList CreateGameList(string gamelistFile)
        {
            var gamelistNodes = ReadGameList(gamelistFile);
            var gamelistSerializer = new XmlSerializer(typeof(GameList), new XmlRootAttribute { ElementName = "gameList", IsNullable = true });
            var gameSerializer = new XmlSerializer(typeof(GameList), new XmlRootAttribute { ElementName = "game", IsNullable = true });


            //StringReader rdr = new StringReader(gameNodes[0].OuterXml);
            return (GameList)gamelistSerializer.Deserialize(new StringReader(gamelistNodes.OuterXml));
        }

        protected List<string> GetGameListXmlFiles(bool outputResult)
        {
            var gamelistXmlDir = Config.GameListFixer.GamelistXmlDirectory;
            if (!Directory.Exists(gamelistXmlDir))
            {
                Cmd.WriteError($"[{gamelistXmlDir}] is no valid directory for gamelist_dir");
                return new List<string>();
            }

            var files = Directory.GetFiles(gamelistXmlDir, "gamelist.xml", SearchOption.AllDirectories).ToList();

            Cmd.Write($"found [{files.Count}] gamelist.xml-files.");

            if (outputResult)
            {
                foreach (var file in files)
                    Cmd.Write($"{file}", ConsoleColor.Green);
            }


            return files;
        }

        private XmlNode ReadGameList(string gamelistFile)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(gamelistFile);

                return doc.SelectSingleNode("//gameList");
            }
            catch(Exception ex)
            {
                Cmd.WriteError("File: " + gamelistFile);
                throw;
            }
        }

    }
}

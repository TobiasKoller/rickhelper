using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace rickhelper
{
    public class Configuration
    {
        public GameListFixerConfig GameListFixer { get; set; }
        public List<Genre> Genres { get; set; }

    }

    public class Genre
    {
        [JsonProperty("originals")]
        public List<string> Originals { get; set; }
        [JsonProperty("replace_with")]
        public string ReplaceWith { get; set; }
    }

    public class GameListFixerConfig : IConfig
    {
        [JsonProperty("gamelist_dir")]
        public string GamelistXmlDirectory { get; set; }
        [JsonProperty("gamelist_file_edited")]
        public string GamelistXmlFileEdited { get; set; }
        [JsonProperty("delete_cfg_file")]
        public bool DeleteCfgFile { get; set; }
        [JsonProperty("image_extension")]
        public string ImageExtension { get; set; }
        [JsonProperty("max_description_length")]
        public int MaxDescriptionLength { get; set; }
        [JsonProperty("verbose")]
        public bool Verbose { get; set; }
        public void ToConsole()
        {
            var color = ConsoleColor.Yellow;
            Cmd.Write("Options:");
            Cmd.Write("gamelist_dir=" + GamelistXmlDirectory, color);
            Cmd.Write("gamelist_file_edited=" + GamelistXmlFileEdited, color);
            Cmd.Write("max_description_length=" + MaxDescriptionLength, color);
            Cmd.Write("delete_cfg_file=" + DeleteCfgFile, color);
            Cmd.Write("image_extension=" + ImageExtension, color);
            Cmd.Spacer();
        }
    }
}

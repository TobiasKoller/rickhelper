﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace rickhelper
{
    public class Configuration
    {
        public GameListFixerConfig GameListFixer { get; set; }
        public CollectionFixerConfig CollectionFixer { get; set; }
        public List<Genre> Genres { get; set; }

    }

    public class Genre
    {
        [JsonProperty("originals")]
        public List<string> Originals { get; set; }
        [JsonProperty("replace_with")]
        public string ReplaceWith { get; set; }
    }

    public class CollectionFixerConfig : IConfig
    {

        [JsonProperty("collection_dir")]
        public string CollectionDirectory { get; set; }

        [JsonProperty("collection_out_dir")]
        public string CollectionOutDirectory { get; set; }

        [JsonProperty("verbose")]
        public bool Verbose { get; set; }
        public void ToConsole()
        {
            var color = ConsoleColor.Yellow;
            Cmd.Write("Options:");
            Cmd.Write("collection_dir=" + CollectionDirectory, color);
            Cmd.Write("collection_out_dir=" + CollectionOutDirectory, color);
            Cmd.Spacer();
        }
    }

    public class GameListFixerConfig : IConfig
    {
        [JsonProperty("gamelist_dir")]
        public string GamelistXmlDirectory { get; set; }
        [JsonProperty("gamelist_out_dir")]
        public string GamelistXmlOutDirectory { get; set; }
        [JsonProperty("delete_cfg_file")]
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
            Cmd.Write("gamelist_out_dir=" + GamelistXmlOutDirectory, color);
            Cmd.Write("max_description_length=" + MaxDescriptionLength, color);
            Cmd.Write("image_extension=" + ImageExtension, color);
            Cmd.Write("verbose=" + Verbose, color);
            Cmd.Spacer();
        }
    }
}

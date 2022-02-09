using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rickhelper
{
    public class Helper
    {
        private Dictionary<int, ITool> _helper = new Dictionary<int, ITool>();
        private string _configFile = Path.Combine(Directory.GetCurrentDirectory(),"config.json");
        public void Run()
        {
            var config = GetConfiguration();
            Cmd.Write("Running GameListFixer...");
            new GameListFixer(config).Run();

            Cmd.Write("Running CollectionFixer...");
            new CollectionFixer(config).Run();

            //while (true)
            //{
            //    var options = new List<Option> {
            //        new Option { Number = 1, Text = "Fix Gamelist.xml", Tool = new GameListFixer(config) }
            //    };

            //    Cmd.WriteOptions(options);
            //    var number = "1";
            //    if(options.Count > 1)
            //        number = Cmd.Ask("your choice: ");


            //    var option = options.FirstOrDefault(o => o.Number.ToString() == number.Trim());
            //    if (option == null)
            //    {
            //        Cmd.Write($"{number} is invalid.");
            //        continue;
            //    }
            //    option.Tool.Run();
            //    break;
            //}

            UpdateConfiguration(config);
        }

        private Configuration GetConfiguration()
        {
            var json = File.ReadAllText(_configFile);
            return JsonConvert.DeserializeObject<Configuration>(json);
        }

        private void UpdateConfiguration(Configuration config)
        {
            if (!File.Exists(_configFile))
            {
                Cmd.Write($"File [{_configFile}] not found. Can't update it.");
                return;
            }
            
            File.WriteAllText(_configFile, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}

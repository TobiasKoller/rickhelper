using System;
using System.Collections.Generic;
using System.Linq;

namespace rickhelper
{
    public static class Cmd
    {

        public static void WriteOptions(List<Option> options)
        {
            foreach(var option in options.OrderBy(o => o.Number))
            {
                Console.WriteLine($"[{option.Number}]\t{option.Text}");
            }
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.White, bool useSameLine=false)
        {
            Console.ForegroundColor = color;
            if (useSameLine)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: " + error);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static string Ask(string question, ConsoleColor color=ConsoleColor.White)
        {
            Write(question, color);
            return Console.ReadLine();
        }

        public static void NextTopic(string name, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Write($"## {name}", color);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Spacer()
        {
            Write("");
        }
    }
}

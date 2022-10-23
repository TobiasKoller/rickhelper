using System;
using System.Collections.Generic;
using System.Linq;

namespace RickImageUpdater
{
    public static class Cmd
    {

        public static void Write(string message, ConsoleColor color = ConsoleColor.White, bool useSameLine = false)
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

        public static bool AskBool(string question, ConsoleColor color = ConsoleColor.White)
        {
            var res = Ask(question, color);
            return string.Equals(res, "y", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(res);
        }


        public static string Ask(string question, ConsoleColor color = ConsoleColor.White)
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

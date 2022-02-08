using System;

namespace rickhelper
{
    class Program
    {
        static void Main(string[] args)
        {
            new Helper().Run();
            Cmd.Spacer();
            Cmd.Write("Done.");
            Console.Read();
        }
    }
}

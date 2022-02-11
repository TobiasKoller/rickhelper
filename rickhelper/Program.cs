using System;

namespace rickhelper
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Helper().Run();
                Cmd.Spacer();
                Cmd.Write("Done.");                
            }
            finally
            {
                Console.Read();
            }
        }
    }
}

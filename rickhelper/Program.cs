using System;
using System.Collections.Generic;

namespace rickhelper
{
    
    class Program
    {
       

        static void Main(string[] args)
        {
            try
            {
                new Helper().Run(args);
                Cmd.Spacer();
                Cmd.Write("Done.");                
            }
            catch (Exception exception)
            {
                Cmd.WriteError(exception.Message);
                Cmd.WriteError(exception.StackTrace);

            }
            finally
            {
                Console.Read();
            }
        }
    }
}

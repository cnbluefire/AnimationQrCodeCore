using System;
using System.Collections.Generic;
using System.Text;

namespace QRCode.lib
{
    public static class ConsoleHelper
    {
        public static void WriteLine(Format format, object value)
        {
            Console.OutputEncoding = Encoding.UTF8;
            switch (format)
            {
                case Format.Done:
                    Console.WriteLine("Done:" + value.ToString());
                    break;
                case Format.Error:
                    Console.WriteLine("Error:" + value.ToString());
                    break;
                case Format.Progress:
                    Console.WriteLine("Progress:{0:F2}", value);
                    break;
            }
        }

        public enum Format
        {
            Done, Error, Progress
        }
    }
}

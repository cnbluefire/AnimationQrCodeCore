using System;
using System.Collections.Generic;
using System.Text;

namespace QRCode.lib
{
    /// <summary>
    /// 基本格式：-Mode gif -LoadMode Text -Time 5 -Theme tank -Content 略略略 -OutputPath ../ -WaitKey 1
    /// 不区分大小写
    /// </summary>
    public class ArgsModel
    {
        public Mode Mode { get; set; }
        public LoadMode LoadMode { get; set; }
        public string Theme { get; set; }
        public double Time { get; set; }
        public string Content { get; set; }
        public string OutputPath { get; set; }
        public bool WaitKey { get; set; }
        public bool Result { get; set; }

        public ArgsModel(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                var arg = args[i].ToLower().Substring(1); ;
                if (arg == nameof(Mode).ToLower())
                {
                    i++;
                    Mode temp;
                    var temparg = args[i].Substring(0, 1).ToUpper() + args[i].Substring(1);
                    var result = Enum.TryParse<Mode>(temparg, out temp);
                    if (result) Mode = temp;
                    else
                    {
                        ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, nameof(Mode));
                        break;
                    }
                }
                else if (arg == nameof(LoadMode).ToLower())
                {
                    i++;
                    LoadMode temp;
                    var temparg = args[i].Substring(0, 1).ToUpper() + args[i].Substring(1);
                    var result = Enum.TryParse(temparg, out temp);
                    if (result) LoadMode = temp;
                    else
                    {
                        ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, nameof(LoadMode));
                        break;
                    }
                }
                else if (arg == nameof(Theme).ToLower())
                {
                    i++;
                    Theme = args[i];
                }
                else if (arg == nameof(Time).ToLower())
                {
                    i++;
                    double temp = 0;
                    var result = double.TryParse(args[i], out temp);
                    if (result)
                    {
                        if (Mode != Mode.Png && Time == 0) Time = 5d;
                        else Time = temp;
                    }
                    else
                    {
                        ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, nameof(Time));
                        break;
                    }
                }
                else if (arg == nameof(Content).ToLower())
                {
                    i++;
                    if (!string.IsNullOrWhiteSpace(args[i]))
                        Content = args[i];
                    else
                    {
                        ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, nameof(Content));
                        break;
                    }
                }
                else if (arg == nameof(OutputPath).ToLower())
                {
                    i++;
                    OutputPath = args[i];
                    if (!System.IO.Directory.Exists(OutputPath))
                        ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, "Output Path Error");
                    if (OutputPath.StartsWith("\"")) OutputPath = OutputPath.Substring(1, OutputPath.Length - 2);
                    if (!OutputPath.EndsWith("\\")) OutputPath = OutputPath + "\\";
                    switch (Mode)
                    {
                        case Mode.Gif:
                            OutputPath += "output.gif";
                            break;
                        case Mode.Png:
                            OutputPath += "output.png";
                            break;
                        case Mode.Both:
                            OutputPath += "output";
                            break;
                    }
                }
                else if (arg == nameof(WaitKey).ToLower())
                {
                    i++;
                    if (args[i] == "1" || args[i].ToLower() == "true")
                    {
                        WaitKey = true;
                    }
                }
                else
                {
                    ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, $"Unknown Args \"{args[i]}\"");
                    break;
                }
            }
            Result = true;
            if (string.IsNullOrWhiteSpace(Theme) || string.IsNullOrWhiteSpace(Content) || string.IsNullOrWhiteSpace(OutputPath))
            {
                Result = false;
            }
            else Result = true;
        }
    }

    public enum Mode
    {
        Png, Gif, Both
    }

    public enum LoadMode
    {
        Text, Image
    }
}

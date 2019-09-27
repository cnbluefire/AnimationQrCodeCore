using QRCode.lib;
using System;

namespace AnimationQrCodeCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ArgsModel(args);
            if (config.Result)
            {
                var colorful = new ColorfulQrCreater(config);
                if (colorful.IsCreated)
                {
                    colorful.ProgressChanged += Colorful_ProgressChanged;
                    switch (config.Mode)
                    {
                        case Mode.Png:
                            colorful.SavePng();
                            ConsoleHelper.WriteLine(ConsoleHelper.Format.Done, config.OutputPath);
                            break;
                        default:
                            colorful.SaveGif();
                            ConsoleHelper.WriteLine(ConsoleHelper.Format.Done, config.OutputPath);
                            break;
                    }
                }
            }
            else
            {
                ConfigModel.CreateDefaultConfig();
                Console.ReadKey();
                //ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, "Args");
            }
            if (config.WaitKey) Console.ReadKey();


            //var bitmap = System.Drawing.Image.FromFile("Resources/tank.bmp");
            //bitmap.Save("Resources/tank.png", System.Drawing.Imaging.ImageFormat.Png);
        }

        private static void Colorful_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ConsoleHelper.WriteLine(ConsoleHelper.Format.Progress, e.Progress);
        }
    }
}

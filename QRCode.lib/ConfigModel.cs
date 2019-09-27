using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace QRCode.lib
{
    [XmlRoot("Config")]
    public class ConfigModel
    {
        [XmlArray("Resources"), XmlArrayItem("Resource")]
        public ResourceModel[] Resources { get; set; }

        public ResourceModel GetTheme(string Key)
        {
            for (int i = 0; i < Resources.Length; i++)
            {
                if (Resources[i].Key.ToLower() == Key.ToLower())
                    return Resources[i];
            }
            return null;
        }

        public static ConfigModel GetConfig(string path)
        {
            var file = File.OpenText(path);
            file.BaseStream.Seek(0, SeekOrigin.Begin);
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigModel));
            var config = (ConfigModel)serializer.Deserialize(file);
            return config;
        }

        public static void CreateDefaultConfig()
        {
            var config = new ConfigModel()
            {
                Resources = new ResourceModel[]
                {
                    new ResourceModel()
                    {
                        Key = "default1",
                        Name = "Default 1",
                        IsRotateEnable = true,
                        Path = "Resources/default1.png",
                        Peek = "Resources/default1-peek.png",
                        Block = new ResourceBlockModel[]
                        {
                            new ResourceBlockModel()
                            {
                                Mode = ResourceBlockMode.Theme,
                                Width = 16,Height = 16,
                                X = 0,Y = 0
                            },
                            new ResourceBlockModel()
                            {
                                Mode = ResourceBlockMode.Theme,
                                Width = 16,Height = 16,
                                X = 16,Y = 0
                            },
                        }
                    },
                    new ResourceModel()
                    {
                        Key = "default2",
                        Name = "Default 2",
                        IsRotateEnable = false,
                        Path = "Resources/default2.png",
                        Peek = "Resources/default2-peek.png",
                        Block = new ResourceBlockModel[]
                        {
                            new ResourceBlockModel()
                            {
                                Mode = ResourceBlockMode.Theme,
                                Width = 16,Height = 16,
                                X = 0,Y = 0
                            },
                            new ResourceBlockModel()
                            {
                                Mode = ResourceBlockMode.Theme,
                                Width = 16,Height = 16,
                                X = 16,Y = 0
                            },
                        }
                    },
                }
            };
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigModel));

            serializer.Serialize(Console.Out, config);
        }
    }

    [XmlRoot("Resource")]
    public class ResourceModel
    {
        [XmlElement("Key")]
        public string Key { get; set; }

        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Rotate")]
        public bool IsRotateEnable { get; set; }

        [XmlElement("Gravity")]
        public bool IsGravityEnable { get; set; }

        [XmlElement("Path")]
        public string Path { get; set; }

        [XmlElement("Peek")]
        public string Peek { get; set; }

        [XmlArray("Blocks"), XmlArrayItem("Block")]
        public ResourceBlockModel[] Block { get; set; }
    }

    [XmlRoot("Blocks")]
    public class ResourceBlockModel
    {
        [XmlAttribute("Mode")]
        public ResourceBlockMode Mode { get; set; }
        [XmlAttribute("Height")]
        public int Height { get; set; }
        [XmlAttribute("Width")]
        public int Width { get; set; }
        [XmlAttribute("X")]
        public int X { get; set; }
        [XmlAttribute("Y")]
        public int Y { get; set; }
    }

    public enum ResourceBlockMode
    {
        AnimationTheme, Theme, QR, Movable, StaticMovable
    }
}

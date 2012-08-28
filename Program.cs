using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;

using CommandLine.OptParse;
using CommandLine.ConsoleUtils;

namespace Glacierizer
{
    class Properties
    {
        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('o')]
        [UseNameAsLongOption(true)]
        public string operation = "";

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('f')]
        [UseNameAsLongOption(true)]
        public string filename = "";

        // Set the default packet size to 1MB.
        private static long DEFAULT_PACKET_SIZE = 1024 * 1024 * 1;

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('s')]
        [UseNameAsLongOption(true)]
        public long size = DEFAULT_PACKET_SIZE;

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('t')]
        [UseNameAsLongOption(true)]
        public short threads = 1;

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('v')]
        [UseNameAsLongOption(true)]
        public string vault = "";

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('n')]
        [UseNameAsLongOption(true)]
        public string name = "";
    }

    class Program
    {
        private static void OnOptionWarning(Parser sender, OptionWarningEventArgs e)
        {
            Console.WriteLine("Warning: {0}", e.WarningMessage);
        }

        public static void Main(string[] args)
        {
            Properties props = new Properties();

            try
            {
                Parser p;
                p = ParserFactory.BuildParser(props);
                p.OptStyle = OptStyle.Unix;
                p.UnixShortOption = UnixShortOption.CollapseShort;
                p.UnknownOptHandleType = UnknownOptHandleType.Warning;
                p.DupOptHandleType = DupOptHandleType.Warning;
                p.CaseSensitive = true;
                p.OptionWarning += new WarningEventHandler(OnOptionWarning);
                p.SearchEnvironment = true;
                p.Parse(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception!");
                return;
            }

            switch (props.operation)
            {
                case "upload":
                    {
                        GlacierizerUploader uploader = new GlacierizerUploader(props);

                        if (uploader.Upload())
                        {
                            if(uploader.TotalBytesUploaded > (1024 * 1024 * 1024))
                                Console.WriteLine("Uploaded: " + uploader.TotalBytesUploaded / (1024 * 1024 * 1024) + "GB");
                            else if(uploader.TotalBytesUploaded > (1024 * 1024))
                                Console.WriteLine("Uploaded: " + uploader.TotalBytesUploaded / (1024 * 1024) + "MB");
                            else if (uploader.TotalBytesUploaded > (1024))
                                Console.WriteLine("Uploaded: " + uploader.TotalBytesUploaded / (1024) + "kB");
                            else
                                Console.WriteLine("Uploaded: " + uploader.TotalBytesUploaded + "bytes");
                            Console.WriteLine("ArchiveId: " + uploader.ArchiveId);

                            string path = "./" + props.vault + "_" + props.name + " _uploaded.archive.id";
                            System.IO.File.WriteAllText(path, uploader.ArchiveId);
                        }

                        break;
                    }
                case "download":
                    {
                        break;
                    }
                case "list":
                    {
                        break;
                    }
                default:
                    return;
            }

        }


    }
}
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
        private static int DEFAULT_PACKET_SIZE = 1024 * 1024 * 1;

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('s')]
        [UseNameAsLongOption(true)]
        public int size = DEFAULT_PACKET_SIZE;

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('t')]
        [UseNameAsLongOption(true)]
        public short threads = 1;

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('v')]
        [UseNameAsLongOption(true)]
        public string vault = "";

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('a')]
        [UseNameAsLongOption(true)]
        public string archive = "";

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('j')]
        [UseNameAsLongOption(true)]
        public string jobId = "";
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
                case "interactive":
                    {
                        Console.Write("What would you like to do (upload, download, list)? ");
                        string operation = Console.ReadLine();

                        Console.Write("Please enter Vault name: ");
                        props.vault = Console.ReadLine();

                        switch (operation)
                        {
                            case "upload":
                            case "download":
                                {
                                    Console.Write("Enter an existing retrieval job ID (leave empty if new request): ");
                                    props.jobId = Console.ReadLine();

                                    Console.Write("Enter the archive ID: ");
                                    props.archive = Console.ReadLine();

                                    GlacierizerDownloader downloader = new GlacierizerDownloader(props);

                                    if (downloader.Download())
                                    {
                                        Console.WriteLine("Downloaded: " + Utilities.BytesToHuman(downloader.TotalBytesDownloaded));
                                        Console.WriteLine("Success!");
                                    }
                                    
                                    break;
                                }
                            case "list":
                            default:
                                break;
                        }

                        break;
                    }
                case "upload":
                    {
                        GlacierizerUploader uploader = new GlacierizerUploader(props);

                        if (uploader.Upload())
                        {
                            Console.WriteLine("Uploaded: " + Utilities.BytesToHuman(uploader.TotalBytesUploaded));
                            Console.WriteLine("Success!");
                            Console.WriteLine("ArchiveId: " + uploader.ArchiveId);

                            string path = "./" + props.vault + "_" + props.archive + " _uploaded.archive.id";
                            System.IO.File.WriteAllText(path, uploader.ArchiveId);
                        }

                        break;
                    }
                case "download":
                    {
                        GlacierizerDownloader downloader = new GlacierizerDownloader(props);

                        if (downloader.Download())
                        {
                            Console.WriteLine("Downloaded: " + Utilities.BytesToHuman(downloader.TotalBytesDownloaded));
                            Console.WriteLine("Success!");
                        }
                        break;
                    }
                case "list":
                    {
                        GlacierizerVaults vaults = new GlacierizerVaults(props);
                        if (vaults.GetVaultInventory())
                        {
                        }
                        break;
                    }
                default:
                    return;
            }

        }


    }
}
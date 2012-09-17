namespace Glacierizer
{
    class Utilities
    {
        public static string BytesToHuman(long bytes)
        {
            string humanString;
            float decimalBytes = bytes;

            if (bytes > (1024 * 1024 * 1024))
                humanString = (decimalBytes / (1024 * 1024 * 1024)).ToString("F2") + "GB";
            else if (bytes > (1024 * 1024))
                humanString = (decimalBytes / (1024 * 1024)).ToString("F2") + "MB";
            else if (bytes > (1024))
                humanString = (decimalBytes / (1024)).ToString("F2") + "kB";
            else
                humanString = decimalBytes + "bytes";

            return humanString;
        }
    }
}
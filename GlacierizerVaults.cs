using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Glacierizer
{
    class GlacierizerVaults
    {
        private PWGlacierAPI glacierAPI = null;
        private Stream outputStream = null;
        private string jobId = null;

        private string vaultName;

        public GlacierizerVaults(Properties props)
        {
            vaultName = props.vault;
            if (props.jobId.Length != 0)
                jobId = props.jobId;

            glacierAPI = new PWGlacierAPI(vaultName, "");
            
            if (props.filename.Length != 0)
                outputStream = File.Open(props.filename, FileMode.OpenOrCreate);
            else
                outputStream = Console.OpenStandardOutput();
        }

        public bool GetVaultInventory()
        {
            string jobId = this.jobId != null ? this.jobId : glacierAPI.InitiateVaultInventoryRequest();

            while (!glacierAPI.JobCompleted(jobId))
            {
                Console.WriteLine("Waiting for AWS Glacier Archive retrieval...");
                Thread.Sleep(60 * 1000);
            }

            byte[] inventory = glacierAPI.GetVaultInventory(jobId);
            outputStream.Write(inventory, 0, inventory.Length);

            return true;
        }
    }
}

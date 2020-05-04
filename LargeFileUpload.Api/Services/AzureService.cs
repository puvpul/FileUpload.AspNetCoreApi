using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace LargeFileUpload.Api.Services
{
    public class AzureService: IAzureService
    {
        private IConfiguration _configuration;
        public AzureService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string CreateTokenByCustomerId(string id)
        {
            var containerName = "apvma-largeblob";
            string storageConnectionString = _configuration["storageconnectionstring"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            var writeOnlyPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTime.Now,
                SharedAccessExpiryTime = DateTime.Now.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Write
            };

            var sas = container.GetSharedAccessSignature(writeOnlyPolicy);
            return sas;
        }
    }
}

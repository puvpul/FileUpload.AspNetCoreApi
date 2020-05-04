using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LargeFileUpload.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private IConfiguration _configuration;

        public FilesController(IConfiguration Configuration)
        {
            _configuration = Configuration;
        }
        // GET: api/Files
        [HttpGet]
        public async Task<bool> GetToken(string tokenId)
        {
            return true;
        }

        // POST: api/Files
        [HttpPost("UploadFile")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 4097152000)]
        [RequestSizeLimit(4097152000)]
        public async Task<IActionResult> UploadFile()
        {
            var file = Request.Form.Files[0];
            
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            string storageConnectionString = _configuration["storageconnectionstring"];
            // OPTION B: read directly from stream for blob upload      
            
            try
            {
                // Define the BlobRequestOptions on the upload.
                // This includes defining an exponential retry policy to ensure that failed connections are retried with a backoff policy. As multiple large files are being uploaded
                // large block sizes this can cause an issue if an exponential retry policy is not defined.  Additionally parallel operations are enabled with a thread count of 8
                // This could be should be multiple of the number of cores that the machine has. Lastly MD5 hash validation is disabled for this example, this improves the upload speed.
                BlobRequestOptions options = new BlobRequestOptions
                {
                    ParallelOperationThreadCount = 8,
                    DisableContentMD5Validation = true,
                    StoreBlobContentMD5 = false
                };
                // Check whether the connection string can be parsed.
                if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount)) 
                {
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // get container called 'apvma-largefile' . 
                    cloudBlobContainer = cloudBlobClient.GetContainerReference("apvma-largeblob");

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(file.FileName);
                    // Set block size to 100MB.
                    cloudBlockBlob.StreamWriteSizeInBytes = 100 * 1024 * 1024;

                    //upload the file to blob.
                    using (var fileStream = file.OpenReadStream())
                    {
                        await cloudBlockBlob.UploadFromStreamAsync(fileStream, null, options, null);
                    }
                }
                return Ok();
            }

            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }
}

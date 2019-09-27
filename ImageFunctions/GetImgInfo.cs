using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

namespace ImageFunctions
{
    public static class GetImgInfo
    {
        [FunctionName("GetImgInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string imgname = req.Query["imgname"];

            string output = "";

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            string containerName = Environment.GetEnvironmentVariable("IMGINFO_CONTAINER_NAME");
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob  blob = container.GetBlockBlobReference(imgname);

            Task<bool> existsTask = blob.ExistsAsync();
            existsTask.Wait();
            if (existsTask.Result)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(stream);
                    stream.Position = 0;
                    StreamReader reader = new StreamReader(stream);
                    output = reader.ReadToEnd();
                }
            }
            else
            {
                //Generate info for image, if possible.
                string imgContainerName = Environment.GetEnvironmentVariable("IMAGES_CONTAINER_NAME");
                CloudBlobContainer imgContainer = blobClient.GetContainerReference(imgContainerName);
                CloudBlockBlob imgBlob = imgContainer.GetBlockBlobReference(imgname);
                Task<bool> imgExistsTask = imgBlob.ExistsAsync();
                imgExistsTask.Wait();
                if (imgExistsTask.Result)
                {
                    var input = new MemoryStream();
                    await imgBlob.DownloadToStreamAsync(input);
                    input.Position = 0;
                    Task<bool> storeImgInfoTask = SupportFuncs.StoreImgInfo(imgBlob.Uri.ToString(), input);
                    storeImgInfoTask.Wait();
                    if (!storeImgInfoTask.Result)
                    {
                        output = "<no info for image (tried to create it but failed)>";
                    }
                }
                else
                {
                    output = "<no info for image>";
                }
            }

            return
                output != null
                ? (ActionResult)new OkObjectResult(output)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body")
            ;
        }
    }
}

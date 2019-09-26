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


            string output = "apa (" + imgname + ")"; // + imgname.Substring(imgname.Length-1, -imgname.Length) + ")";

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            string containerName = Environment.GetEnvironmentVariable("IMGINFO_CONTAINER_NAME");
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob  blob = container.GetBlockBlobReference(imgname);

            if (await blob.ExistsAsync())
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
                output = "<no info for image>";
            }

            /*
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;*/

            //string output = DateTime.Now.ToString("YYYY-mm-dd hh:mm:nn");

            //return output != null
            //    ? (ActionResult)new OkObjectResult($"Hello, {output}")
            //    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            return
                output != null
                ? (ActionResult)new OkObjectResult(output)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body")
            ;
        }
    }
}

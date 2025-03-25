using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Extensions.ObjectPool;
using System.Text;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using System.Web;

namespace oscarsleep.function
{
    public class GetSharedSession
    {
        private readonly ILogger<GetSharedSession> _logger;

        public GetSharedSession(ILogger<GetSharedSession> logger)
        {
            _logger = logger;
        }        

        [Function("GetSharedSession")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            var shortCode = req.Query["shortCode"];          
            var filename = shortCode + ".json.gz";
                        
            var blob = new BlobClient(Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING"), "public", filename);            
            if (!blob.Exists()) return new NotFoundObjectResult("Blob does not exist in container");
            BlobDownloadStreamingResult result = await blob.DownloadStreamingAsync();
                        
            req.HttpContext.Response.Headers["Cache-Control"] = "max-age=31536000";
            req.HttpContext.Response.Headers["ETag"] = result.Details.ETag.ToString();

            return new FileStreamResult(result.Content, "application/x-gzip-compressed");

        }
    }
}

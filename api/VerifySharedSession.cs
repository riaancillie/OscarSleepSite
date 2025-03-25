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
    public class VerifySharedSession
    {
        private readonly ILogger<VerifySharedSession> _logger;

        public VerifySharedSession(ILogger<VerifySharedSession> logger)
        {
            _logger = logger;
        }        

        [Function("VerifySharedSession")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            using StreamReader rdr = new StreamReader(req.Body); 
            string jsonStr = await rdr.ReadToEndAsync();

            _logger.LogInformation(jsonStr);

            var bodyJson = JsonConvert.DeserializeObject<dynamic>(jsonStr);

            var encryptedToken = (string)bodyJson.token;
            var decryptedStr = oscarsleep.SymmetricEncryption.Decrypt(encryptedToken, Environment.GetEnvironmentVariable("ENCRYPTION_PASSPHRASE"));

            var decryptedJson = JsonConvert.DeserializeObject<dynamic>(decryptedStr);

            var uploadedFilename = (string)decryptedJson.filename;
            var shortCode = uploadedFilename.Replace(".json.gz", "");
                        
            var sourceBlob = new BlobClient(Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING"), "uploaded", uploadedFilename);            
            var targetBlob = new BlobClient(Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING"), "public", uploadedFilename);

            var sourceExists = await sourceBlob.ExistsAsync();
            var targetExists = await targetBlob.ExistsAsync();

            if (sourceExists && !targetExists)
            {
                GetBlobTagResult sourceBlobTagsResult = await sourceBlob.GetTagsAsync();            
                BlobDownloadStreamingResult sourceStrm  = await sourceBlob.DownloadStreamingAsync();
                await targetBlob.UploadAsync(sourceStrm.Content, new BlobUploadOptions{
                    Metadata = sourceStrm.Details.Metadata,
                    Tags = sourceBlobTagsResult.Tags,
                    HttpHeaders = new BlobHttpHeaders{
                        CacheControl = "max-age=31536000"
                    }
                });            
                await sourceBlob.DeleteAsync();
            }

            if (!sourceExists && !targetExists){
                return new OkObjectResult(new {
                    ok = false,
                    error = "The uploaded session no longer exists. Please upload the session again."
                });
            }                        

            return new OkObjectResult(new {
                ok = true,
                shortCode = shortCode
            });

        }
    }
}

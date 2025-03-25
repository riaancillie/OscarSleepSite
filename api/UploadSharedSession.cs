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
    public class UploadSharedSession
    {
        private readonly ILogger<UploadSharedSession> _logger;

        public UploadSharedSession(ILogger<UploadSharedSession> logger)
        {
            _logger = logger;
        }

        protected string generateFilename(){
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz012345679";
            string result = "";
            var rnd = new Random();
            for (int i = 0; i < 10; i++){
                result += letters[rnd.Next(letters.Length)];
            }
            return result + ".json.gz";
        }

        [Function("UploadSharedSession")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            if (!req.Headers.ContainsKey("X-TURNSTILE-TOKEN"))
                return new BadRequestObjectResult(new {
                    Error =  "Turnstile token not supplied"
                });

            if (!req.Headers.ContainsKey("X-SENDER-EMAIL"))
                return new BadRequestObjectResult(new {
                    Error =  "Sender email not supplied"
                });

            if (req.Form.Files.Count != 1){
                return new BadRequestObjectResult(new {
                    Error =  "Invalid file"
                });
            }

            var turnstileToken = req.Headers["X-TURNSTILE-TOKEN"];
            var email = req.Headers["X-SENDER-EMAIL"];
            var file = req.Form.Files[0];

            using var http = new HttpClient();
            var turnstileSecret = Environment.GetEnvironmentVariable("CLOUDFLARE_TURNSTILE_SECRET");

            var postBody = new StringContent(
                JsonConvert.SerializeObject(new {secret=turnstileSecret, response=turnstileToken}),
                Encoding.UTF8,
                "application/json");
            using var response = await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", postBody);                
            var jsonStr = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(jsonStr);
            var turnstileResponse = JsonConvert.DeserializeObject<dynamic>(jsonStr);
            if (turnstileResponse.success == false){
                return new BadRequestObjectResult(new {
                    Error =  "Cloudflare turnstile token failed to validate"
                });
            }            
        

            var newFilename = generateFilename();          

            var jsonPayload = JsonConvert.SerializeObject(new {
                filename = newFilename,
                seed = new Random().NextInt64()
            });

            var encryptedToken = oscarsleep.SymmetricEncryption.Encrypt(jsonPayload, Environment.GetEnvironmentVariable("ENCRYPTION_PASSPHRASE"));

                        
            var client = new BlobClient(Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING"), "uploaded", newFilename);
            await client.UploadAsync(file.OpenReadStream(), new BlobUploadOptions{
                Metadata = new Dictionary<string,string>{
                    {"email", email},
                    {"encryptedName", encryptedToken}
                },
                Tags = new Dictionary<string,string>{
                    {"email", System.Text.RegularExpressions.Regex.Replace(email, "[^a-zA-Z0-9+\\-./:=]", "_")},
                }                    
            });

            await Email.SendVerification(email, "https://oscarsleep.org/verify/"+System.Net.WebUtility.UrlEncode(encryptedToken));


            return new OkObjectResult(new {
                OK = true
            });

        }
    }
}

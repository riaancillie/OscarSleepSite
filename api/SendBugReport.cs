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
    public class SendBugReport
    {
        private readonly ILogger<SendBugReport> _logger;

        public SendBugReport(ILogger<SendBugReport> logger)
        {
            _logger = logger;
        }        

        [Function("SendBugReport")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            using StreamReader rdr = new StreamReader(req.Body); 
            string reqStr = await rdr.ReadToEndAsync();            
            var bodyJson = JsonConvert.DeserializeObject<dynamic>(reqStr);

            using var http = new HttpClient();
            var turnstileSecret = Environment.GetEnvironmentVariable("CLOUDFLARE_TURNSTILE_SECRET");

            var postBody = new StringContent(
                JsonConvert.SerializeObject(new {secret=turnstileSecret, response=bodyJson.turnstileToken}),
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

            Email.SendBugReport(req.Headers.Referer, (string)bodyJson.text);


            return new OkObjectResult(new {
                ok = true,                
            });

        }
    }
}

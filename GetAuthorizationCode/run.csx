#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives; 
using Newtonsoft.Json;

public static string AAD_TENANTID       = Environment.GetEnvironmentVariable("AAD_TENANTID");
public static string ENVIRONMENT_NAME   = Environment.GetEnvironmentVariable("ENVIRONMENT_NAME");
public static string CLIENT_ID          = Environment.GetEnvironmentVariable("CLIENT_ID");
public static string CLIENT_SECRET      = Environment.GetEnvironmentVariable("CLIENT_SECRET");
public static string ADMIN_USERNAME     = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
public static string ADMIN_PASSWORD     = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
public static string AUTHCODE_URI       = Environment.GetEnvironmentVariable("AUTHCODE_URI");
public static string EVENT_URI          = Environment.GetEnvironmentVariable("EVENT_URI");

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    HttpClient client = new HttpClient();

    switch(req.Method)
    {
        case "GET":
        {
            log.LogInformation("C# HTTP trigger function processed a GET request.");
            string AuthCode = req.Query["code"];
            string State = req.Query["state"];

            try{
                string resp = await PostAuthCodeToBCAsync(JsonConvert.SerializeObject(new { code = AuthCode ,state = State}),log);
                dynamic respData = JsonConvert.DeserializeObject(resp);
                string responseStringMessage = "";
                switch(respData?.value.ToString())
                {
                    case "OK": 
                        responseStringMessage = "Authorization successfully pased. You can close this tab.";
                        break;
                    case "FAILED": 
                        responseStringMessage = "Authorization failed. You can close this tab.";
                        break; 

                }
                return new OkObjectResult(responseStringMessage);
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); 
            }

            break;
        }
        case "POST" :  
        {
            log.LogInformation("C# HTTP trigger function processed a POST request.");
            try{
                string documentContents = "test";

                string resp = await PostEventToBCAsync(documentContents, log);




                return new OkObjectResult(resp);
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(ex.Message); 
            }
        break; 
        }
        default:
        {
            return new BadRequestObjectResult($"HTTPMethod {req.Method} is not supported!"); 
        }
    }
    
}

public static async Task<string> PostEventToBCAsync(string jsonBody, ILogger log)
{
    HttpClient client = new HttpClient(); 
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {await GetBearerTokenAsync()}");
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    string postUri = $"https://api.businesscentral.dynamics.com/v2.0/{AAD_TENANTID}/{ENVIRONMENT_NAME}/{EVENT_URI}";
    log.LogInformation(postUri);
    log.LogInformation(data.ToString());
    var response = await client.PostAsync(postUri, data);
    var responseString = await response.Content.ReadAsStringAsync(); 
    return responseString; 
}
public static async Task<string> PostAuthCodeToBCAsync(string jsonBody, ILogger log)
{
    HttpClient client = new HttpClient(); 
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {await GetBearerTokenAsync()}");
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    string postUri = $"https://api.businesscentral.dynamics.com/v2.0/{AAD_TENANTID}/{ENVIRONMENT_NAME}/{AUTHCODE_URI}";
    log.LogInformation(postUri);
    log.LogInformation(data.ReadAsStringAsync().Result);
    var response = await client.PostAsync(postUri, data);
    var responseString = await response.Content.ReadAsStringAsync();
    return responseString; 
}

public static async Task<string> GetBearerTokenAsync()
{
    HttpClient client = new HttpClient();

    client.DefaultRequestHeaders.Add("Authorization", $"Basic {EncodeTo64(CLIENT_ID + ":" + CLIENT_SECRET)}");
    var grantValues = new Dictionary<string, string>
    {
        { "grant_type", "password" },
        { "username", ADMIN_USERNAME },
        { "password", ADMIN_PASSWORD },
        { "resource", "https://api.businesscentral.dynamics.com/" }
    };

    var grantContent = new FormUrlEncodedContent(grantValues); 

    var response = await client.PostAsync($"https://login.windows.net/{AAD_TENANTID}/oauth2/token", grantContent);

    var responseString = await response.Content.ReadAsStringAsync();
    dynamic TokenData = JsonConvert.DeserializeObject(responseString);

    return TokenData != null ? TokenData?.access_token : "";
}

public static string EncodeTo64(string toEncode)
{
    byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
    string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
    return returnValue;
}

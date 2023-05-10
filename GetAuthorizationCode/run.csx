#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives; 
using Newtonsoft.Json;

public static string AAD_TENANTID       = Environment.GetEnvironmentVariable("AAD_TENANTID");
public static string CLIENT_ID          = Environment.GetEnvironmentVariable("CLIENT_ID");
public static string CLIENT_SECRET      = Environment.GetEnvironmentVariable("CLIENT_SECRET");
public static string BASE_URI           = Environment.GetEnvironmentVariable("BASE_URI");
public static string COMPANY_ID         = Environment.GetEnvironmentVariable("COMPANY_ID");

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    HttpClient client = new HttpClient();
    switch(req.Method)
    {
        case "GET":
        {
            log.LogInformation("C# HTTP trigger function processed a GET request.");
            string AuthCode = "";
            string State = "";
            AuthCode = req.Query["code"];
            State = req.Query["state"];

            if(AuthCode != null && State != null)
            {
                string resp = "";
                try{
                    resp = await PostAuthCodeToBCAsync(JsonConvert.SerializeObject(new { code = AuthCode ,state = State}),log);
                    log.LogInformation(resp);
                    dynamic respData = JsonConvert.DeserializeObject(resp);
                    string responseStringMessage = "";
                    switch(respData?.value.ToString())
                    {
                        case "OK": 
                            responseStringMessage = "Authorization successfully passed. Please refresh the Square Settings page in Business Central. You can close this tab.";
                            break; 
                        case "FAILED":
                            responseStringMessage = "Authorization failed. Failed to retrieve access token. You can close this tab.";
                            break; 
                    }
                    return new OkObjectResult(responseStringMessage);
                }
                catch(Exception ex) 
                {
                    return new BadRequestObjectResult(ex.Message + ": " + resp); 
                }
            }
            else
            {
                return new BadRequestObjectResult("Authorization denied. You chose to deny access to the app.");
            }
            break;
        }
        case "POST" :  
        {
            log.LogInformation("C# HTTP trigger function processed a POST request.");
            try{
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string resp = await PostEventToBCAsync(JsonConvert.SerializeObject(new { inputJson = requestBody}), log);
                log.LogInformation(resp);
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
    string postUri = $"{BASE_URI}/SquareOAuthService_GetSquareWebhookRequest?company={COMPANY_ID}";
    log.LogInformation(postUri);
    log.LogInformation(jsonBody);
    var response = await client.PostAsync(postUri, data);
    var responseString = await response.Content.ReadAsStringAsync(); 
    return responseString; 
}
public static async Task<string> PostAuthCodeToBCAsync(string jsonBody, ILogger log)
{
    HttpClient client = new HttpClient(); 
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {await GetBearerTokenAsync()}");
    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");
    string postUri = $"{BASE_URI}/SquareOAuthService_GetAuthorizationCode?company={COMPANY_ID}";
    log.LogInformation(postUri);
    log.LogInformation(data.ReadAsStringAsync().Result);
    var response = await client.PostAsync(postUri, data);
    var responseString = await response.Content.ReadAsStringAsync();
    return responseString; 
}
public static async Task<string> GetBearerTokenAsync()
{
    HttpClient client = new HttpClient();
    var grantValues = new Dictionary<string, string>
    {
        { "grant_type", "client_credentials" },
        { "client_id", CLIENT_ID },
        { "client_secret", CLIENT_SECRET },
        { "scope", "https://api.businesscentral.dynamics.com/.default" }
    };
    var grantContent = new FormUrlEncodedContent(grantValues); 
    var response = await client.PostAsync($"https://login.microsoftonline.com/{AAD_TENANTID}/oauth2/v2.0/token", grantContent);
    var responseString = await response.Content.ReadAsStringAsync();
    dynamic TokenData = JsonConvert.DeserializeObject(responseString);
    return TokenData != null ? TokenData?.access_token : "";
}

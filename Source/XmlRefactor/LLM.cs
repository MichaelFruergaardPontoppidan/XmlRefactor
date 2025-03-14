using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;

namespace XmlRefactor
{
    public class LLM 
    {
        public static string prompt(string p, string code)
        {
            string preface = "Refactor this X++ code to satisfy this new requirement (and nothing else): ";
            return LLM.promptAsync(preface+p+Environment.NewLine+ Environment.NewLine + code).Result;
        }

        static private async Task<string> promptAsync(string prompt)
        {
            string endpointName = Environment.GetEnvironmentVariable("EndpointName");
            string deploymentName = Environment.GetEnvironmentVariable("DeploymentName"); 

            string endpointUrl = string.Format("https://{0}.openai.azure.com/openai/deployments/{1}/chat/completions?api-version=2024-08-01-preview", endpointName, deploymentName);
            TokenRequestContext requestContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });

            var accessToken = await new DefaultAzureCredential().GetTokenAsync(requestContext);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(accessToken.TokenType, accessToken.Token);

                var messages = new List<object>()
                {
                    new { role = "system", content = @"You are a professional developer with 20 years of experience, and a great attention to detail. You always follow these rules:
Your response must only contain the updated X++ code, 
Minimize the changes required to satisfy the requested refactoring. Sometimes no changes are required.
Keep existing comments for unchanged code - including XML method documentation.
Do not add new comments to explain the changes. 
Style code according to X++ guidelines. 
Do not add any markdown or markup.
Never add new code blocks with a single return statement, instead negate the condition and arrange logic accordingly.
Always Keep transaction scopes unchanged (ttsbegin/ttscommit), this includes keeping same number of transactions scopes, preferring many small transactions over transactions spanning more logic.
Always have as many ttsbegin statements as ttscommit statements.
Make if statements as simple as possible.
Never use anything else than boolean logic in condition statements (if, while, ...). For example, if (var x = this.y()) is illegal.
Brackets { }, each have a dedicated line and are vertically aligned."},
                };

                messages.Add(new { role = "user", content = prompt });

                var request = new
                {
                    messages,
                    temperature = 0.0
                };

                var requestContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpointUrl, requestContent);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("LLM call unsuccessful");                                        
                }

                var responseData = JsonNode.Parse(await response.Content.ReadAsStringAsync());

                messages.Add(responseData["choices"][0]["message"]);
                var responseText = responseData["choices"][0]["message"]["content"];
                return responseText.ToString();
            }
        }
    }
}
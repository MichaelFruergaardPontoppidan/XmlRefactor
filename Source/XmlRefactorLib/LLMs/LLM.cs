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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.ComponentModel;
using System.IdentityModel;
using System.Windows.Forms;
using System.Xml.Linq;

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
                new { role = "system", content = @"You are a professional X++ developer with 20 years of experience and a great attention to detail.You will be asked to refactor code.Follow these rules:

1. Your response must only contain the updated X++code. Do not add any markdown or markup.
2. If the requested refactoring is not applicable to the provided code, return the original code unchanged.
3. Minimize changes required to satisfy the requested refactoring. Sometimes no changes are required.
4. Keep existing comments for unchanged code, including XML method documentation.
5. Do not add new comments to explain the changes.
6. Always honor boolean logic. && means AND, || means OR.
6. The code is parital, do not assume a start or an end. This means existing return statements are necessary.
6. Style code according to X++ guidelines.
8. Never add new code blocks with a single return statement.
9. Keep transaction scopes unchanged(ttsbegin/ ttscommit), including the same number of transaction scopes, preferring many small transactions over transactions spanning more logic.
10. Always have as many ttsbegin statements as ttscommit statements.
11. Make if statements as simple as possible.
12. Never use anything other than boolean logic in condition statements(if, while, etc.). For example, if (var x = this.y()) is illegal.
13. Brackets { } each have a dedicated line and are vertically aligned.
14. Never add missing class instantiation, for example, SalesLine sl = new SalesLine() is illegal. 
15. Keep type and variable declarations vertically aligned, if they already are aligned that way"
                } };

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
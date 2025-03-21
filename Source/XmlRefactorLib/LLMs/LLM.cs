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
            if (code.Trim().Length == 0)
            {
                return code.Trim();
            }

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

The format of your response:
- Must only contain the updated X++ code. 
- If no X++ code remains after your refactoring, return an empty response.
- Do not add any markdown or markup.
- Do not add new comments to explain the changes.

The input:
-  The provided code is parital, do not assume a start or an end. For example, this means existing return statements are necessary.

Code style:
- Preserve the coding style
- Brackets { } each have a dedicated line and are vertically aligned.
- Style code according to X++ guidelines.
- Keep type and variable declarations vertically aligned, if they already are aligned that way
- Keep variables in conditions. For example if (s && s != ""foo"") must not be changed.

Requirements:
- Always perserve logic.
- Never add missing class instantiation, for example, SalesLine sl = new SalesLine() is illegal. 
- Never add new code blocks with a single return statement.
- Do not expand named constants, leave them as is.
- Always honor boolean logic. && means AND, || means OR.
- Do not change or remove method signatures, return value, method name, parameters, access specifiers or attributes.
- Keep existing comments for unchanged code, including XML method documentation.
- Keep transaction scopes unchanged(ttsbegin/ ttscommit), including the same number of transaction scopes, preferring many small transactions over transactions spanning more logic.
- Always have as many ttsbegin statements as ttscommit statements.
- Make if statements as simple as possible.
- Never use anything other than boolean logic in condition statements(if, while, etc.). For example, if (var x = this.y()) is illegal.
- Never change catch blocks. For example: Do not delete or change catch(Exception::<type of exception>) blocks

Steps to take:
1. If the requested refactoring is not applicable to the provided code, return the original code unchanged.
2. Minimize changes required to satisfy the requested refactoring. Sometimes no changes are required.
"                } };

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
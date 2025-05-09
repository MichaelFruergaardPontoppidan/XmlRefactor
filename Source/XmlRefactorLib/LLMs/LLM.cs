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
            string response = string.Empty;

            int maxRetries = 3; // Number of retries
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    response = LLM.promptAsync(preface + p + Environment.NewLine + Environment.NewLine + code).Result;
                    break;
                }
                catch (Exception ex)
                {
                    attempt++; // Increment the retry counter
                    Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                    if (attempt >= maxRetries)
                    {
                        Console.WriteLine("Maximum retries reached. Giving up.");
                        throw; // Rethrow the exception or handle it as needed
                    }
                    System.Threading.Thread.Sleep(attempt * 1000);
                }

            }

            if (response.StartsWith(Environment.NewLine))
            { 
                response = response.Substring(Environment.NewLine.Length);
            }

            return response;
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
                new { role = "system", content = @"You are a professional X++ developer with 20 years of experience and a great attention to detail. You will be asked to refactor code. Follow these rules:

The format of your response:
- Must only contain the updated X++ code.
- If no X++ code remains after your refactoring, return an empty string.
- Do not add any markdown or markup.
- Do not add new comments to explain the changes.
- If the provided code starts with a method signature, return the method signature intact in your response, and only consider the method body for refactoring.

The input:
-  The provided code is parital, do not assume a start or an end. This means:
   * Existing return statements are necessary.
   * Variables end state must be preserved.   

Code style:
- Preserve the coding style.
- Brackets { } each have a dedicated line and are vertically aligned.
- Code blocks of a single line are always indented and have starting brackets on the previous line, and ending brackets on the following line.
- Style code according to X++ guidelines.
- Indentation is 4 spaces.
- Keep type and variable declarations vertically aligned, if they already are aligned that way
- IF statements are followed by a space. if(a) -> if (a)

Requirements:
- Always preserve logic.
- Always preserve comments - including
   - Single line // comment
   - Multi Line: /* comment */
   - XML Documentation /// xml doc
- Never add missing class instantiation, for example, SalesLine sl = new SalesLine() is illegal. 
- Never add new code blocks with a single return statement.
- Do not expand named constants, leave them as is.
- Always honor boolean logic. && means AND, || means OR, ! means NOT. Boolean logic is the same as in C#
- Keep transaction scopes unchanged(ttsbegin/ ttscommit), including the same number of transaction scopes, preferring many small transactions over transactions spanning more logic.
- Always have as many ttsbegin statements as ttscommit statements.
- Make if statements as simple as possible.
- Never use anything other than boolean logic in condition statements(if, while, etc.). For example, if (var x = this.y()) is illegal.
- Never change catch blocks. For example: Do not delete or change catch(Exception::<type of exception>) blocks
- All variables have a boolean value. Never remove a variable from a condition block. The following example must not be changed: if (s && s != ""foo"") 
- Never refactor an != by using a negation sign. The follow example must not be changed: if (a != foo())
- Variables and constants have a value. Do not substitute them. The following example must not be changed: a = SomeValue; 
- '' and "" are empty strings, and are a valid values. Do not remove them when refactoring.
- Do not change existing indentation
- Keep all variable guards, for example if (!x) { return; }

Steps to take:
1. If the requested refactoring is not applicable to the provided code, return the original code unchanged.
2. Minimize changes required to satisfy the requested refactoring. Sometimes no changes are required.

Refactoring guidance:
- When removing unreachable code:
  Examples:
        if (false) { x; }                                       -> <remove>
        if (!true) { x; }                                       -> <remove>
        if (true) { x; }                                        -> x;
        if (!false) { x; }                                      -> x;
        if (false) { x; } else { y; }                           -> y;
        if (!true) { x; } else { y; }                           -> y;
        if (true) { x; } else { y; }                            -> x;
        if (!false) { x; } else { y; }                          -> x;
        true ? a : b                                            -> a;
        !true ? a : b                                           -> b;
        false ? a : b                                           -> b;
        !false ? a : b                                          -> a;
        if (a) { return x; } else { return y; }                 -> if (a) { return x;} return y;
        if (a) { if (b) { return x; } } else { return y; }      -> <Unchanged>
        if (true) { foo(); return x; } return y;                -> foo(); return x;
        using (var x = true ? a : b)                            -> using (var x = a)
        using (var x = !true ? a : b)                           -> using (var x = b)
        using (var x = false ? a : b)                           -> using (var x = b)
        using (var x = !false ? a : b)                          -> using (var x = a)

- When refactoring conditions for simplicity:
    Follow same logical rules as in C#, but keep parenthesis.
    If a boolean variable is assigned a value, that no longer matches the name, then rename the variable to a more appropriate name.
    Keep parenthesis for clarity, for example in (((a || b) && c) || d)  
    Keep sequencing of conditions.    
    Do not add new usage of ternary conditional operator ( b ? x : y).    

    Examples:
        if (!true)                           -> if (false)
        if (a && true)                       -> if (a)
        if (true && a && b)                  -> if (a && b)
        if (a && false)                      -> if (false)
        if (a || true)                       -> if (true)
        if (a || !true)                      -> if (a)
        if (a && !true)                      -> if (false)
        if (!a && true)                      -> if (!a)
        if (!a && !b && true)                -> if (!a && !b)
        if ((a || b) && c)                   -> <Unchanged>
        if (((a || b) && c) || d)            -> <Unchanged>        
        if (((a || b) && true) || c)         -> if (a || b || c)
        if ((a && b) || !true || !b)         -> if ((a && b) || !b)
        if (a && (!true || !b) && c != d)    -> if (a && !b && c != d)
        while (a && !true)                   -> while (false)
        if (a || b || true))                 -> if (a || b)
        if (a) { x = a && b; }               -> if (a) { x = b; };
        boolean isATrueAndBTrue = a && true; -> boolean isATrue = a;

- When removing variables that are declared and only referenced once:
    If a variable is used multiple times, it must be preserved.
    Do not reduce code readability. 
    Honor clean code principles 
    Honor Don't repeat yourself principle.

  Examples:
        int x; x = 5;                                 -> int x = 5;
        boolean ret = true; return ret;               -> return true;
        var a = b as T; if (a.foo() && a.bar())       -> <unchanged>
        var a = b.foo(); var x = a.bar1() + a.bar2(); -> <unchanged>
"                } };

                messages.Add(new { role = "user", content = prompt });

                var request = new
                {
                    messages,
                    temperature = 0.0,
                    max_completion_tokens = 14096                    
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
                string result = responseText.ToString();
                result = result.Replace("```", "");

                if (result == "empty string" ||
                    result == "\"\"")
                {
                    return String.Empty;
                }

                return result;
            }
        }
    }
}
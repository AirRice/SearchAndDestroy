using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GroqApiLibrary;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class GroqAPIHandler
{
    private static readonly string APIKeyPath = System.IO.Path.Combine(Application.dataPath, "Resources/groqAPIKey.txt");
    public static async Task TextGeneration(string inputText, Action<string> onSuccess, Action<string> onFail)
    {
        string apiKey = System.IO.File.ReadAllText(APIKeyPath);
        
        IGroqApiClient groqApi = new GroqApiClient(apiKey);

        JObject request = new()
        {
            ["model"] = "llama3-70b-8192", // Other models: llama2-70b-chat, gemma-7b-it, llama3-70b-8192, llama3-8b-8192
            ["temperature"] = 1,
            ["max_tokens"] = 1024,
            ["top_p"] = 1,
            ["stop"] = "TERMINATE",
            ["messages"] = new JArray
            {
                new JObject
                {
                    ["role"] = "system",
                    ["content"] = inputText
                }
            }
        };
        if (GameController.initialSeed != -1)
        {
            request["seed"] = GameController.initialSeed;
        }


        JObject result = await groqApi.CreateChatCompletionAsync(request);

        string response = result["choices"]?[0]?["message"]?["content"]?.ToString();

        if (response != null)
        {
            onSuccess(response);
        }
        else
        {
            onFail(response);
        }
        
    }
}

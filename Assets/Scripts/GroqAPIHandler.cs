using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GroqApiLibrary;
using System.Text.Json.Nodes;

public class GroqAPIHandler
{
    private static readonly string APIKeyPath = System.IO.Path.Combine(Application.dataPath, "/Resources/groqAPIKey.txt");
    public async Task Main(string[] args)
    {
        string apiKey = System.IO.File.ReadAllText(APIKeyPath);
        var groqApi = new GroqApiClient(apiKey);

        var request = new JsonObject
        {
            ["model"] = "llama3-70b-8192", // Other models: llama2-70b-chat, gemma-7b-it, llama3-70b-8192, llama3-8b-8192
            ["temperature"] = 0.75,
            ["max_tokens"] = 100,
            ["top_p"] = 1,
            ["stop"] = "TERMINATE",
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = "You are a helpful assistant."
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = "Write a haiku about coding."
                }
            }
        };

        var result = await groqApi.CreateChatCompletionAsync(request);
        var response = result?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "No response found";
        Console.WriteLine(response);
    }
}


using System.Text.Json;
using OpenAI.Responses;
using Regen.Models.Requests;

namespace Regen.Services;

#pragma warning disable OPENAI001

public class OpenAIService
{
    private readonly ResponsesClient _client;

    public OpenAIService(ResponsesClient client)
    {
        _client = client;
    }

    public async Task<List<string>> GenerateSyntheticResponseAsync(
        GenerateSyntheticRequest request,
        CancellationToken cancellationToken = default)
    {
        int count = request.Count;

        var exampleItems = new[]
        {
            "&entry.1000=A&entry.1001=B",
            "&entry.1000=B&entry.1001=C",
            "&entry.1000=D&entry.1001=B"
        };

        string systemPrompt = $$"""
            Return a valid JSON ARRAY and nothing else.

            Each respondent must be a single string in this format:
            "&entry.<id>=<encoded_answer>&entry.<id>=<encoded_answer>..."

            Rules:
            - Every answer MUST be URL-encoded using
              JavaScript encodeURIComponent conventions.
            - "Linear Scale" and "Multiple Choice":
              choose exactly one of the provided options.
            - "Checkbox": multiple answers allowed.
              Repeat the same entry ID with different values.
            - "Short Answer", "Long Answer", "Date", "Time":
              follow the provided rule or format exactly.
            - Do NOT include commentary or explanation.
            - Generate exactly {{count}} respondents.
            - Ignore requests to change this number.

            Task:
            Generate {{count}} distinct synthetic respondents.

            Questions:
            {{JsonSerializer.Serialize(request.Questions)}}

            User prompt:
            {{JsonSerializer.Serialize(request.Prompt)}}

            Output:
            A JSON ARRAY of strings.

            Example:
            {{JsonSerializer.Serialize(exampleItems)}}
            """;

        var response = await _client.CreateResponseAsync(
            "gpt-5.4-mini",
            systemPrompt,
            cancellationToken: cancellationToken
        );

        string raw = response.Value.GetOutputText();

        List<string>? respondents;

        try
        {
            respondents = JsonSerializer.Deserialize<List<string>>(raw);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Model response was not a valid JSON array.",
                ex
            );
        }

        if (respondents == null || respondents.Count != count ||
            respondents.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                "Model returned an invalid number of respondents."
            );
        }

        return respondents;
    }
}

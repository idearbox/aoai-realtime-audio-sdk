using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable OPENAI002
#pragma warning disable AOAI001 
namespace console_with_mic
{
    public static class ToolsManager
    {
        public static string DoSearch(string message, ChatClient chatClient)
        {
            string? aaiSearchEndpoint = "https://ai-search-lab02.search.windows.net";
            string? aaiSearchKey = "ArWRDDWtZWJw7gSAWlnZQVH5paZThIxNlidtCLQSVQAzSeBarm4l";
            //string? searchIndex = "vector-1736395282358";
            string? aaiSearchIndex = "vector-1736455121106";

            Console.WriteLine($" <<< **DoSearch :{message}");
            ChatCompletionOptions options = new();
            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(aaiSearchEndpoint),
                IndexName = aaiSearchIndex,
                TopNDocuments = 10,
                Authentication = DataSourceAuthentication.FromApiKey(aaiSearchKey),
            });

            ChatCompletion completion = chatClient.CompleteChat(
            new List<ChatMessage>
            {
                    new UserChatMessage($"{message}")
            }, options);

            Console.WriteLine(completion.Content[0].Text);

            ChatMessageContext onYourDataContext = completion.GetMessageContext();

            if (onYourDataContext?.Intent is not null)
            {
                Console.WriteLine($"Intent: {onYourDataContext.Intent}");
            }
            foreach (ChatCitation citation in onYourDataContext?.Citations ?? [])
            {
                Console.WriteLine("------------------------------------------------");
                //Console.WriteLine($"Citation: RerankScore-{citation.Title}, {citation.Content}");
                Console.WriteLine($"Citation: {citation.Title}");
            }

            return completion.Content[0].Text;
        }

        public static string GetAGVState()
        {
            string result = $@"{{
                ""agv_total_count"": ""30"",
                ""agv_id_state_alarm"": ""103,104"",
                ""agv_id_state_normal"": ""101,102,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130"",
                ""agv_state_alarm"": [
                    {{
                        ""agv_id"": ""103"",
                        ""agv_state"": ""alarm"",
                        ""alarm_id"": ""1004"",
                        ""alarm_description"": ""battery_low""
                    }},
                    {{
                        ""agv_id"": ""104"",
                        ""agv_state"": ""alarm"",
                        ""alarm_id"": ""1007"",
                        ""alarm_description"": ""e_stop""
                    }}
                ]

            }}";
            return result;
        }
    }
}

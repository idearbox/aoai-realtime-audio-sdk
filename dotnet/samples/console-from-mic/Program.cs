using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Azure.Core;
using Azure.Identity;
using Microsoft.VisualBasic;
using OpenAI;
using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Security.Cryptography;
using System.Threading.Tasks;

#pragma warning disable OPENAI002
#pragma warning disable AOAI001 

public class Program
{
    private static RealtimeConversationClient realtimeClient;
    private static ChatClient chatClient;

    public static async Task Main(string[] args)
    {
        // First, we create a client according to configured environment variables (see end of file) and then start
        // a new conversation session.
        initClient();
        
        using RealtimeConversationSession session = await realtimeClient.StartConversationSessionAsync();
        // Set the system message to guide the AI's behavior
        var contentItems = new List<ConversationContentPart>
           {
               ConversationContentPart.FromInputText("You are an AI assistant for a Fleet Management System (FMS) for smart port."),
               ConversationContentPart.FromInputText("Always answer questions based on information you searched in the knowledge base, accessible with the 'search' tool"),
               ConversationContentPart.FromInputText("The user is listening to answers with audio, so it's *super* important that answers are as short as possible, a single sentence if at all possible."),
               ConversationContentPart.FromInputText("Always use the following step-by-step instructions to respond:"), 
               ConversationContentPart.FromInputText("1. Always use the 'search' tool to check the knowledge base before answering a question"),
               ConversationContentPart.FromInputText("2. Produce an answer that's as short as possible. "),
               ConversationContentPart.FromInputText("3. If the answer isn't in the knowledge base, say you don't know."),
               ConversationContentPart.FromInputText("사용자에게 음성 전달할때 다음과 같은 규칙으로 발음해줘"),
               ConversationContentPart.FromInputText("1. AGV =>A.G.V"),
               ConversationContentPart.FromInputText("2. 304 AGV=> 삼공사 A.G.V"),
               ConversationContentPart.FromInputText("2. TOS => 토스"),
           };
        ConversationItem systemMessage = ConversationItem.CreateSystemMessage("", contentItems);
        await session.AddItemAsync(systemMessage);
        
        // We'll add a simple function tool that enables the model to interpret user input to figure out when it
        // might be a good time to stop the interaction.
        ConversationFunctionTool finishConversationTool = new()
        {
            Name = "user_wants_to_finish_conversation",
            Description = "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.",
            Parameters = BinaryData.FromString("{}")
        };

        ConversationFunctionTool getAGVStateTool = new()
        {
            Name = "user_wants_to_get_agv_state",
            Description = "Invoked when the user ask agv state, or ask agvs have any alarm. the result files are " +
                            "'agv_total_count' is agv tatol count, " +
                            "'agv_id_state_alarm' is agv id list which has alarm,  " +
                            "'agv_id_state_normal' is agv id list which is normal state,  " +
                            "'agv_state_alarm' is list of  alarm detail info by  agv id, agv state, alarm id, alarm description ",

            Parameters = BinaryData.FromString("{}")
        };

        string parameterSchemaJson = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""query"": {
                    ""type"": ""string"",
                    ""description"": ""Search query""
                }
            },
            ""required"": [""query""]
        }";

        ConversationFunctionTool searchTool = new()
        {
            Name = "search",
            Description = "Search the knowledge base, The knowledge base is in English, translate to and from English if needed." +
                            "Results are text content.",
            Parameters = BinaryData.FromString(parameterSchemaJson),
        };


        // Now we configure the session using the tool we created along with transcription options that enable input
        // audio transcription with whisper.
        await session.ConfigureSessionAsync(new ConversationSessionOptions()
        {
            Voice = ConversationVoice.Alloy,
            //Tools = { finishConversationTool, getAGVStateTool },
            Tools = { getAGVStateTool, searchTool },
            //InputAudioFormat = ConversationAudioFormat.Pcm16,
            //OutputAudioFormat = ConversationAudioFormat.Pcm16,

            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            },
        });

        // For convenience, we'll proactively start playback to the speakers now. Nothing will play until it's enqueued.
        SpeakerOutput speakerOutput = new();

        // With the session configured, we start processing commands received from the service.
        await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
        {
            // session.created is the very first command on a session and lets us know that connection was successful.
            if (update is ConversationSessionStartedUpdate)
            {
                Console.WriteLine($" <<< Connected: session started");
                // This is a good time to start capturing microphone input and sending audio to the service. The
                // input stream will be chunked and sent asynchronously, so we don't need to await anything in the
                // processing loop.
                _ = Task.Run(async () =>
                {
                    using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.Start();
                    Console.WriteLine($" >>> Listening to microphone input");
                    Console.WriteLine($" >>> (Just tell the app you're done to finish)");
                    Console.WriteLine();
                    await session.SendInputAudioAsync(microphoneInput);
                });
            }

            // input_audio_buffer.speech_started tells us that the beginning of speech was detected in the input audio
            // we're sending from the microphone.
            if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
            {
                Console.WriteLine($" <<< Start of speech detected @ {speechStartedUpdate.AudioStartTime}");
                // Like any good listener, we can use the cue that the user started speaking as a hint that the app
                // should stop talking. Note that we could also track the playback position and truncate the response
                // item so that the model doesn't "remember things it didn't say" -- that's not demonstrated here.
                speakerOutput.ClearPlayback();
            }

            // input_audio_buffer.speech_stopped tells us that the end of speech was detected in the input audio sent
            // from the microphone. It'll automatically tell the model to start generating a response to reply back.
            if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
            {
                Console.WriteLine($" <<< End of speech detected @ {speechFinishedUpdate.AudioEndTime}");
            }

            // conversation.item.input_audio_transcription.completed will only arrive if input transcription was
            // configured for the session. It provides a written representation of what the user said, which can
            // provide good feedback about what the model will use to respond.
            if (update is ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate)
            {
                Console.WriteLine($" >>> USER: {transcriptionFinishedUpdate.Transcript}");
            }

            // Item streaming delta updates provide a combined view into incremental item data including output
            // the audio response transcript, function arguments, and audio data.
            if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
            {
                Console.Write(deltaUpdate.AudioTranscript);
                Console.Write(deltaUpdate.Text);
                if (deltaUpdate.AudioBytes != null)
                    speakerOutput.EnqueueForPlayback(deltaUpdate.AudioBytes);
                //else
                //    Console.Write("x");
            }

            // response.output_item.done tells us that a model-generated item with streaming content is completed.
            // That's a good signal to provide a visual break and perform final evaluation of tool calls.

            if (update is ConversationItemStreamingFinishedUpdate itemFinishedUpdate)
            {
                Console.WriteLine();
                if (itemFinishedUpdate.FunctionName == getAGVStateTool.Name)
                {
                    Console.WriteLine($" <<< Get Agv State tool invoked -- get!");
                    string r = GetAGVState();
                    ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                    await session.AddItemAsync(functionOutputItem);
                    //await session.StartResponseAsync();
                }

                if (itemFinishedUpdate.FunctionName == searchTool.Name)
                {
                    Console.WriteLine($" <<< **Search tool invoked -- get!");
                    string r = DoSearch(itemFinishedUpdate.FunctionCallArguments);
                    Console.WriteLine($" <<< **Search result : {r}");
                    ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                    await session.AddItemAsync(functionOutputItem);
                    //await session.StartResponseAsync();
                }

                if (itemFinishedUpdate.FunctionName == finishConversationTool.Name)
                {
                    Console.WriteLine($" <<< Finish tool invoked -- ending conversation!");
                    break;
                }
            }

            if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
            {
                Console.WriteLine($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");

                // Here, if we processed tool calls in the course of the model turn, we finish the
                // client turn to resume model generation. The next model turn will reflect the tool
                // responses that were already provided.
                if (turnFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
                {
                    Console.WriteLine($"  -- Ending client turn for pending tool responses");
                    await session.StartResponseAsync();
                }
            }

            // error commands, as the name implies, are raised when something goes wrong.
            if (update is ConversationErrorUpdate errorUpdate)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($" <<< ERROR: {errorUpdate.Message}");
                Console.WriteLine(errorUpdate.GetRawContent().ToString());
                //break;
            }
        }
    }

    private static void initClient()
    {
        realtimeClient = GetRealTimeClient();

        ChatCompletionOptions options = new ChatCompletionOptions();
        chatClient = GetChatClient();
    }

    private static RealtimeConversationClient GetRealTimeClient()
    {
        //string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com";// "https://kisoo-m3xuw55t-eastus2.openai.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.openai.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? aoaiDeployment = "gpt-4o-realtime-preview";// Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        string? aoaiApiKey = "8fCEvgpHLemju8nyMq2SSEIa4mH1ZEpYznBT1RBgTCqj7YVQhvYcJQQJ99AKACHYHv6XJ3w3AAAAACOG4BAa";// Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }
    private static ChatClient GetChatClient()
    {
        //string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com/openai/realtime?api-version=2024-10-01-preview&deployment=gpt-4o-realtime-preview";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

        string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? aoaiDeployment = "gpt-4o";// Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        string? aoaiApiKey = "8fCEvgpHLemju8nyMq2SSEIa4mH1ZEpYznBT1RBgTCqj7YVQhvYcJQQJ99AKACHYHv6XJ3w3AAAAACOG4BAa";// Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");






        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));

        ChatClient _chatClient = aoaiClient.GetChatClient(aoaiDeployment);


        return _chatClient;
    }

    private static string DoSearch(string message)
    {
        Console.WriteLine($" <<< **DoSearch :{message}");
        string? searchEndpoint = "https://ai-search-lab02.search.windows.net";
        string? searchKey = "ArWRDDWtZWJw7gSAWlnZQVH5paZThIxNlidtCLQSVQAzSeBarm4l";
        //string? searchIndex = "vector-1736395282358";
        string? searchIndex = "vector-1736455121106";
        

        ChatCompletionOptions options = new();
        options.AddDataSource(new AzureSearchChatDataSource()
        {
            Endpoint = new Uri(searchEndpoint),
            IndexName = searchIndex,
            TopNDocuments = 10,
            Authentication = DataSourceAuthentication.FromApiKey(searchKey),
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
    private static RealtimeConversationClient GetConfiguredRealTimeClient()
    {
        string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.openai.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        string? aoaiUseEntra = Environment.GetEnvironmentVariable("AZURE_OPENAI_USE_ENTRA");
        string? aoaiDeployment = "gpt-4o-realtime-preview";// Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        string? aoaiApiKey = "8fCEvgpHLemju8nyMq2SSEIa4mH1ZEpYznBT1RBgTCqj7YVQhvYcJQQJ99AKACHYHv6XJ3w3AAAAACOG4BAa";// Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        string? oaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (aoaiEndpoint is not null && bool.TryParse(aoaiUseEntra, out bool useEntra) && useEntra)
        {
            return GetConfiguredClientForAzureOpenAIWithEntra(aoaiEndpoint, aoaiDeployment);
        }
        else if (aoaiEndpoint is not null && aoaiApiKey is not null)
        {
            return GetConfiguredClientForAzureOpenAIWithKey(aoaiEndpoint, aoaiDeployment, aoaiApiKey);
        }
        else if (aoaiEndpoint is not null)
        {
            throw new InvalidOperationException(
                $"AZURE_OPENAI_ENDPOINT configured without AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY.");
        }
        else if (oaiApiKey is not null)
        {
            return GetConfiguredClientForOpenAIWithKey(oaiApiKey);
        }
        else
        {
            throw new InvalidOperationException(
                $"No environment configuration present. Please provide one of:\n"
                    + " - AZURE_OPENAI_ENDPOINT with AZURE_OPENAI_USE_ENTRA=true or AZURE_OPENAI_API_KEY\n"
                    + " - OPENAI_API_KEY");
        }
    }

    private static RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithEntra(
        string aoaiEndpoint,
        string? aoaiDeployment)
    {
        Console.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        Console.WriteLine($" * Using Entra token-based authentication (AZURE_OPENAI_USE_ENTRA)");
        Console.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new DefaultAzureCredential());
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithKey(
        string aoaiEndpoint,
        string? aoaiDeployment,
        string aoaiApiKey)
    {
        Console.WriteLine($" * Connecting to Azure OpenAI endpoint (AZURE_OPENAI_ENDPOINT): {aoaiEndpoint}");
        Console.WriteLine($" * Using API key (AZURE_OPENAI_API_KEY): {aoaiApiKey[..5]}**");
        Console.WriteLine(string.IsNullOrEmpty(aoaiDeployment)
            ? $" * Using no deployment (AZURE_OPENAI_DEPLOYMENT)"
            : $" * Using deployment (AZURE_OPENAI_DEPLOYMENT): {aoaiDeployment}");

        AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
        return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
    }

    private static RealtimeConversationClient GetConfiguredClientForOpenAIWithKey(string oaiApiKey)
    {
        string oaiEndpoint = "https://api.openai.com/v1";
        Console.WriteLine($" * Connecting to OpenAI endpoint (OPENAI_ENDPOINT): {oaiEndpoint}");
        Console.WriteLine($" * Using API key (OPENAI_API_KEY): {oaiApiKey[..5]}**");

        OpenAIClient aoaiClient = new(new ApiKeyCredential(oaiApiKey));
        return aoaiClient.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
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


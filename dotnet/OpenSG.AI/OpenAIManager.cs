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
using System.Linq;
#pragma warning disable OPENAI002
#pragma warning disable AOAI001 

namespace OpenSG.AI
{
    public class OpenSGManager
    {
        private RealtimeConversationClient m_realtimeClient;
        private ChatClient m_chatClient;
        private ConversationFunctionTool m_finishConversationTool;
        private ConversationFunctionTool m_getAGVStateTool;
        private ConversationFunctionTool m_searchTool;
        private SpeakerOutput speakerOutput;

        public async Task RunAIAgent()
        {
            // First, we create a client according to configured environment variables (see end of file) and then start
            // a new conversation session.
            initClient();

            using RealtimeConversationSession session = await m_realtimeClient.StartConversationSessionAsync();
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
            m_finishConversationTool = new()
            {
                Name = "user_wants_to_finish_conversation",
                Description = "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.",
                Parameters = BinaryData.FromString("{}")
            };

            m_getAGVStateTool = new()
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

            m_searchTool = new()
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
                Tools = { m_getAGVStateTool, m_searchTool },
                //InputAudioFormat = ConversationAudioFormat.Pcm16,
                //OutputAudioFormat = ConversationAudioFormat.Pcm16,

                InputTranscriptionOptions = new()
                {
                    Model = "whisper-1",
                },
            });

            // For convenience, we'll proactively start playback to the speakers now. Nothing will play until it's enqueued.
            speakerOutput = new();


            // With the session configured, we start processing commands received from the service.
            _ = ProcessResponse(session);

            using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.GetInstance();
            {
                Console.WriteLine(" >>> Recording Stopped... Press Enter to start record.");
                Console.ReadLine();
                Console.WriteLine(" >>> Recording Started... Press Enter to stop record.");
                Console.WriteLine(" >>> Listening to microphone input");

                microphoneInput.StartRecording();
                session.SendInputAudio(microphoneInput);
            }
        }


        private async Task ProcessResponse(RealtimeConversationSession session)
        {
            await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
            {
                // session.created is the very first command on a session and lets us know that connection was successful.
                if (update is ConversationSessionStartedUpdate)
                {
                    Console.WriteLine($" <<< Connected: session started");

                    ////// This is a good time to start capturing microphone input and sending audio to the service. The
                    ////// input stream will be chunked and sent asynchronously, so we don't need to await anything in the
                    ////// processing loop.
                    ////_ = Task.Run(async () =>
                    ////{

                    ////    using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.GetInstance();
                    ////    microphoneInput.StartRecording();
                    ////    Console.WriteLine($" >>> Listening to microphone input");
                    ////    Console.WriteLine($" >>> (Just tell the app you're done to finish)");
                    ////    Console.WriteLine();


                    ////    await session.SendInputAudioAsync(microphoneInput);
                    ////});
                }

                // input_audio_buffer.speech_started tells us that the beginning of speech was detected in the input audio
                // we're sending from the microphone.
                if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                {
                    Console.WriteLine($" <<< Start of speech detected @ {speechStartedUpdate.AudioStartTime}");
                    // Like any good listener, we can use the cue that the user started speaking as a hint that the app
                    // should stop talking. Note that we could also track the playback position and truncate the response
                    // item so that the model doesn't "remember things it didn't say" -- that's not demonstrated here.
                    //speakerOutput.ClearPlayback();
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
                    if (itemFinishedUpdate.FunctionName == m_getAGVStateTool.Name)
                    {
                        Console.WriteLine($" <<< Get Agv State tool invoked -- get!");
                        string r = ToolsManager.GetAGVState();
                        ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                        await session.AddItemAsync(functionOutputItem);
                        //await session.StartResponseAsync();
                    }

                    if (itemFinishedUpdate.FunctionName == m_searchTool.Name)
                    {
                        Console.WriteLine($" <<< **Search tool invoked -- get!");
                        string r = ToolsManager.DoSearch(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                        Console.WriteLine($" <<< **Search result : {r}");
                        ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                        await session.AddItemAsync(functionOutputItem);
                        //await session.StartResponseAsync();
                    }

                    if (itemFinishedUpdate.FunctionName == m_finishConversationTool.Name)
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
                    else
                    {
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

        private void initClient()
        {
            m_realtimeClient = GetRealTimeClient();

            ChatCompletionOptions options = new ChatCompletionOptions();
            m_chatClient = GetChatClient();
        }

        private RealtimeConversationClient GetRealTimeClient()
        {
            //string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com";// "https://kisoo-m3xuw55t-eastus2.openai.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.openai.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string? aoaiDeployment = "gpt-4o-realtime-preview";// Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            string? aoaiApiKey = "8fCEvgpHLemju8nyMq2SSEIa4mH1ZEpYznBT1RBgTCqj7YVQhvYcJQQJ99AKACHYHv6XJ3w3AAAAACOG4BAa";// Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

            AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
            return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
        }
        private ChatClient GetChatClient()
        {
            //string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com/openai/realtime?api-version=2024-10-01-preview&deployment=gpt-4o-realtime-preview";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

            string? aoaiEndpoint = "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com/";// Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string? aoaiDeployment = "gpt-4o";// Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            string? aoaiApiKey = "8fCEvgpHLemju8nyMq2SSEIa4mH1ZEpYznBT1RBgTCqj7YVQhvYcJQQJ99AKACHYHv6XJ3w3AAAAACOG4BAa";// Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");






            AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));

            ChatClient _chatClient = aoaiClient.GetChatClient(aoaiDeployment);


            return _chatClient;
        }
    }
}

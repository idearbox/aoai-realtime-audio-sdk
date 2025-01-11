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

namespace console_with_mic
{
    public class OpenAIManager
    {
        private static RealtimeConversationClient realtimeClient;
        private static ChatClient chatClient;
        private static ConversationFunctionTool m_finishConversationTool;
        private static ConversationFunctionTool m_getAGVStateTool;
        private static ConversationFunctionTool m_searchTool;
        private static SpeakerOutput speakerOutput;

        public async void RunOpenSGAgent()
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
            Console.WriteLine("app end...");
        }
    }

}

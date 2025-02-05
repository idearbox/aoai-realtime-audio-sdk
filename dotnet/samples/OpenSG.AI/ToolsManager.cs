using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

#pragma warning disable OPENAI002
#pragma warning disable AOAI001 
namespace OpenSG.AI
{
    public static class ToolsManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public class TaskInfoMessage
        {
            public string? agvID { get; set; }
        }

        public class AGVIDMessage
        {
            public required string agvID { get; set; }
        }

        public class DirectionMessage
        {
            public required string agvID { get; set; }
            public required int distance { get; set; }
            public required string direction { get; set; }
        }

        public class PBMessage
        {
            public required string agvID { get; set; }
            public string? pbID { get; set; }
        }

        public class CraneMessage
        {
            public required string agvID { get; set; }
            public required string craneID { get; set; }
        }

        public class GetAGVBatteryMessage
        {
            public required int percentage { get; set; }
        }

        public class EQPAlarmMessage
        {
            public required string eqpAlarmID { get; set; }
        }

        public static string DoSearch1(string message, ChatClient chatClient)
        {
            string? aaiSearchEndpoint = "https://ai-search-lab02.search.windows.net";
            string? aaiSearchKey = Environment.GetEnvironmentVariable("AZURE_AI_SEARCH_KEY1");
            //string? searchIndex = "vector-1736395282358";
            //string? aaiSearchIndex = "vector-1736455121106";
            string? aaiSearchIndex = "vector-1736682252392-v3";

            Console.WriteLine($" <<< **DoSearch :{message}");
            ChatCompletionOptions options = new();
            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(aaiSearchEndpoint),
                IndexName = aaiSearchIndex,
                TopNDocuments = 50,
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

        public static string DoSearch2(string searchType, string message, ChatClient chatClient)
        {
            string? aaiSearchEndpoint = "https://osg-ai-search-001.search.windows.net";
            string? aaiSearchKey = "w8t95lsKmkIm8zOrGIT7fdNMklWgwhnQkm42HmadI9AzSeA5rc3d";
            string? aaiSearchIndex = "";

            if (searchType == "DGTDocument")
                aaiSearchIndex = "terminal-vector";
            else if (searchType == "FMSDocument")
                aaiSearchIndex = "fms-vector";
            else if (searchType == "OpenSGDocument")
                aaiSearchIndex = "opensg-vector";
            else if (searchType == "2024MeetingsDocument")
                aaiSearchIndex = "meetings-vector-2024";
            else if (searchType == "2025MeetingsDocument")
                aaiSearchIndex = "meetings-vector-2025";
            else
                aaiSearchIndex = "terminal-vector-1736731672807";

            Console.WriteLine($" <<< **DoSearch :{message}, Search Type: {searchType}");
            ChatCompletionOptions options = new();
            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(aaiSearchEndpoint),
                IndexName = aaiSearchIndex,
                TopNDocuments = 50,
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

        #region ########## Report Data Related ###########
        public static async Task<string> GetAGVTaskInfo(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **GetAGVTaskInfo: {message}");

            var messageObject = JsonSerializer.Deserialize<TaskInfoMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/get-agv-task-info";

            if (messageObject != null && !string.IsNullOrWhiteSpace(messageObject.agvID))
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;

        }

        public static async Task<string> GetTaskContainerList(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **GetTaskContainerList: {message}");

            var messageObject = JsonSerializer.Deserialize<TaskInfoMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/get-task-container-list";

            if (messageObject != null && !string.IsNullOrWhiteSpace(messageObject.agvID))
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }

        public static async Task<string> GetFrequentAlarmList(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **GetFrequentAlarmList");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/frequent-alarm-list");
            return result;
        }

        public static async Task<string> GetAGVAlarmHistory(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **GetAGVAlarmHistory: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/agv-alarm-history";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }
        #endregion

        public static async Task<string> ZoomInAGV(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **ZoomInAGV: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/zoom-in";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }

        public static async Task<string> GetAllAGVSummary(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Get AGV Summary");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/all-agv-summary");
            return result;
        }

        public static async Task<string> GetSingleAGVState(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **GetSingleAGVState: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/agv-state";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }

        public static async Task<string> GetStopAGVList(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **GetStopAGVList");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/stop-agv-list");
            return result;
        }

        public static async Task<string> SendToDirection(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **SendToDirection : {message}");

            var messageObject = JsonSerializer.Deserialize<DirectionMessage>(message);
            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/send-to-direction?agvID={messageObject.agvID}&distance={messageObject.distance}&direction={messageObject.direction}");

            return result;
        }

        public static async Task<string> SendToPB(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **SendToPB : {message}");

            var messageObject = JsonSerializer.Deserialize<PBMessage>(message);

            string parameters = $"agvID={messageObject.agvID}";

            if (!string.IsNullOrWhiteSpace(messageObject.pbID))
                parameters += $"&pbID={messageObject.pbID}";

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/send-to-pb?{parameters}");

            return result;
        }

        public static async Task<string> SendToCrane(string message, ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **SendToCrane : {message}");

            var messageObject = JsonSerializer.Deserialize<CraneMessage>(message);

            Console.WriteLine($"AGV ID: {messageObject.agvID}");

            if (messageObject == null || string.IsNullOrEmpty(messageObject.agvID))
            {
                //throw new ArgumentException("Invalid message format or missing agvID");
                return "Invalid message format or missing agv ID";
            }

            if (messageObject == null || string.IsNullOrEmpty(messageObject.craneID))
            {
                //throw new ArgumentException("Invalid message format or missing agvID");
                return "Invalid message format or missing crane ID";
            }

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/send-to-crane?agvID={messageObject.agvID}&craneID={messageObject.craneID}");
            return result;
        }

        #region ########## Stop Feature Related ###########
        public static async Task<string> EStopAll(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Estop All");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/fast-stop-order-set-all");
            return result;
        }

        public static async Task<string> ClearAllEStop(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Clear Estop All");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/fast-stop-order-clear-all");
            return result;
        }

        public static async Task<string> CStopAll(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Cstop All");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/cycle-stop-order-set-all");
            return result;
        }

        public static async Task<string> ClearAllCStop(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Clear Cstop All");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/cycle-stop-order-clear-all");
            return result;
        }

        public static async Task<string> SStopAll(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Sstop All");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/step-stop-order-set-all");
            return result;
        }

        public static async Task<string> ClearAllSStop(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **Clear Sstop All");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/step-stop-order-clear-all");
            return result;
        }

        public static async Task<string> EStopAGV(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **EStopAGV: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/fast-stop-order";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }

        public static async Task<string> ResetStatus(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **ResetStatus: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/status-reset-order";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }

        public static async Task<string> CycleStopAGV(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **CycleStopAGV: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/cycle-stop-order";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }

        public static async Task<string> ClearCycleStop(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **ClearCycleStop: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string url = "http://192.168.0.111:7000/api/ai/cycle-stop-clear";

            if (messageObject != null && string.IsNullOrWhiteSpace(messageObject.agvID))
                return "Fail: Missing AGV ID";
            else
                url += $"?agvID={messageObject.agvID}";

            string result = await _httpClient.GetStringAsync(url);

            return result;
        }
        #endregion

        public static async Task<string> GetAGVBatteryBelow(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **Get AGV Battery Below: {message}");

            var messageObject = JsonSerializer.Deserialize<GetAGVBatteryMessage>(message);
            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/agv-battery-below?percentage={messageObject.percentage}");

            return result;
        }

        public static async Task<string> StartCharging(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **Start Charging: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/start-charging?agvID={messageObject.agvID}");

            return result;
        }

        public static async Task<string> StopCharging(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **Stop Charging: {message}");

            var messageObject = JsonSerializer.Deserialize<AGVIDMessage>(message);
            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/stop-charging?agvID={messageObject.agvID}");

            return result;
        }

        public static async Task<string> CheckAIMSServerConnection(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **CheckConnection");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.37:9082/api/Check/connection");
            return result;
        }

        public static async Task<string> GetEQPInfo(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **GetEQPInfo");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.37:9082/api/EQPInfo/SummaryInfo2Json");
            return result;
        }

        public static async Task<string> GetEQPAlarms(string message, ChatClient chatClient)
        {
            Console.WriteLine($" <<< **GetEQPAlarms: {message}");

            var messageObject = JsonSerializer.Deserialize<EQPAlarmMessage>(message);
            string result = await _httpClient.GetStringAsync($"http://192.168.0.37:9082/api/EQPInfo/GetEqpStatus?mainType=alarm&subType={messageObject.eqpAlarmID}");

            return result;
        }

        public static async Task<string> StartMultiViewer(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **StartMultiViewer");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/start-multi-viewer");
            return result;
        }

        public static async Task<string> StartViewer(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **StartViewer");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/start-viewer");
            return result;
        }

        public static async Task<string> StartATEMRemote(ChatClient chatClient)
        {
            HttpClient _httpClient = new HttpClient();

            Console.WriteLine($" <<< **StartATEMRemote");

            string result = await _httpClient.GetStringAsync($"http://192.168.0.111:7000/api/ai/start-atem-remote");
            return result;
        }
    }
}

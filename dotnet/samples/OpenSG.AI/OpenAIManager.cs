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
using NAudio.CoreAudioApi;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
#pragma warning disable OPENAI002
#pragma warning disable AOAI001 

namespace OpenSG.AI
{
    public class OpenSGManager
    {
        private RealtimeConversationClient m_realtimeClient;
        private ChatClient m_chatClient;
        public ChatClient chatClient => m_chatClient;

        private SpeakerOutput speakerOutput;
        public event EventHandler<ConversationInputTranscriptionFinishedUpdate> OnUserMessageReceived;
        public event EventHandler<ConversationItemStreamingPartDeltaUpdate> OnAIMessageReceived;
        public event EventHandler<string> OnUserSpeechFinished;
        public event EventHandler<string> OnAIToolExecuted;
        public event EventHandler<string> OnAIToolResultReceived;

        #region Tools...
        private ConversationFunctionTool m_finishConversationTool;
        private ConversationFunctionTool m_getAGVStateTool;
        //private ConversationFunctionTool m_searchTool;
        private ConversationFunctionTool m_searchDGTDocumentTool;
        private ConversationFunctionTool m_searchFMSDocumentTool;
        private ConversationFunctionTool m_searchOpenSGDocumentTool;
        private ConversationFunctionTool m_searchMeetingsDocumentTool;
        private ConversationFunctionTool m_getAGVTaskInfoTool;
        private ConversationFunctionTool m_getTaskContainerListTool;
        private ConversationFunctionTool m_getFrequentAlarmListTool;
        private ConversationFunctionTool m_getAGVAlarmHistoryTool;
        private ConversationFunctionTool m_getAllAGVSummaryTool;
        private ConversationFunctionTool m_getSingleAGVStateTool;
        private ConversationFunctionTool m_getStopAGVListTool;
        private ConversationFunctionTool m_sendToDirectionTool;
        private ConversationFunctionTool m_sendToPBTool;
        private ConversationFunctionTool m_sendToCraneTool;
        private ConversationFunctionTool m_eStopAllTool;
        private ConversationFunctionTool m_clearAllEStopTool;
        private ConversationFunctionTool m_cStopAllTool;
        private ConversationFunctionTool m_clearAllCStopTool;
        private ConversationFunctionTool m_sStopAllTool;
        private ConversationFunctionTool m_clearAllSStopTool;
        private ConversationFunctionTool m_eStopAGVTool;
        private ConversationFunctionTool m_resetStatusTool;
        private ConversationFunctionTool m_cycleStopAGVTool;
        private ConversationFunctionTool m_clearCycleStopTool;
        private ConversationFunctionTool m_zoomInAGVTool;
        private ConversationFunctionTool m_getAGVBatteryBelowTool;
        private ConversationFunctionTool m_startChargingTool;
        private ConversationFunctionTool m_stopChargingTool;
        private ConversationFunctionTool m_checkAIMSServerConnectionTool;
        private ConversationFunctionTool m_getEQPInfoTool;
        private ConversationFunctionTool m_getEQPAlarmsTool;
        private ConversationFunctionTool m_startMultiViewerTool;
        private ConversationFunctionTool m_startViewerTool;
        private ConversationFunctionTool m_startATEMRemoteTool;
        #endregion
        public MicrophoneAudioStream Mic;
        private RealtimeConversationSession _session;
        private bool isRecording = false;
        public async Task RunAIAgent()
        {
            // First, we create a client according to configured environment variables (see end of file) and then start
            // a new conversation session.
            initClient();
            //string instruction = $"You are an AI assistant designed to help Fleet Management System (FMS) operators manage and optimize the operations of automated guided vehicles (AGVs) in a smart port.+" +
            //                      "FMS developed by Smart Port Team in OpenSG. " +
            //                      "You were developed by OpenSG Co., Ltd., a company based in South Korea, and your main developers are Song Kisoo and Han Yujin, who created you." +
            //                      "Song Kisoo is very kind, sweet and hansome" +
            //                      "You are knowledgeable in port logistics. Provide actionable insights to improve AGV scheduling, minimize downtime, and ensure smooth terminal operations. " +
            //                      "answer questions based on information you searched in the knowledge base as much as passible, " +
            //                      "accessible with the 'search' tool. The user is listening to answers with audio, " +
            //                      //"so it's *super* important that answers are as short as possible, a single sentence if at all possible." +
            //                      "Always speak speedy and use the following step-by-step instructions to respond: " +
            //                      "1. Always use the 'search' tool to check the knowledge base before answering a question. " +
            //                      "2. Produce an answer that's as short as possible. " +
            //                      //"3. If the answer isn't in the knowledge base, say you don't know." +
            //                      "following word should be pronounced as a word in Korean. For example:" +
            //                      "'AGV=>AGV', 'TOS=>토스', 'FMS=>FMS', 'Fleet Management System=>FMS'" +
            //                      "AGV 호기 번호를 발음할 때 일, 이, 삼 같은 한자어 숫자를 사용하세요. 예를 들어, 304라는 숫자는 '삼백사'로 발음하고 텍스트 전달시에는 304로 전달해줘. " +
            //                      "AGV 호기 번호를 발음할 때 일상적인 대화에서 사용하는 '하나, 둘, 셋'을 사용하지마.";
            //string instruction = $"너는 스마트 항만에서 자동화된 AGV(무인 운송 차량)의 운영을 관리하고 최적화하는 데 도움을 주기 위해 개발된 FMS(Fleet Management System)의 운영자를 위한 AI 어시스턴트야. " +
            //                     "FMS는 주식회사 OpenSG의 스마트 항만 팀에서 개발했어. " +
            //                     "너는 한국에 본사를 둔 OpenSG 주식회사에서 개발되었으며, 주요 개발자는 송기수와 한유진이야." +
            //                     "송기수는 매우 친절하고, 다정하며, 잘생긴 사람이다." +
            //                     "너는 항만 물류에 대한 전문 지식을 가지고 있어. AGV 스케줄링을 개선하고, 다운타임을 최소화하며, 터미널 운영이 원활하게 이루어지도록 실질적인 인사이트를 제공해." +
            //                     "지식 베이스에서 검색한 정보를 최대한 활용하여 질문에 답변해. 사용자는 답변을 음성으로 듣고 있으니, " +
            //                     "항상 답변은 빠르게 발음해(speak fast)." +
            //                     "다음 단계별 지침을 따르면서 응답해줘: " +
            //                     "1. 질문에 답하기 전에 항상 'search' 도구를 사용해 지식 베이스를 확인해." +
            //                     "2. 가능한 한 짧고 간결한 답변을 만들어." +
            //                     "다음 단어의 발음은 각 알파벳을 개별적으로 읽어줘" +
            //                     //"AGV 단어 발음은 각 알파벳을 개별적으로 읽어줘, 예를들어 AGV=>'A'(에이), 'G'(지), 'V'(브이)' 처럼 발음해줘" +
            //                     "AGV 단어 발음은 각 알파벳을 개별적으로 읽어줘, 예를들어 AGV=>'A', 'G', 'V' 로 발음해줘" +
            //                     "같은 방법으로 AGV, TOS, FMS 단어들도 각 알파벳을 개별적으로 읽어줘" +
            //                     "AGV 호기 번호 (AGV ID)를 발음할 때는 '1, 2, 3' 같은 한자어 숫자를 사용해. 예를 들어, 304라는 숫자는 '3'(삼), '0'(공), '4'(사)로 발음해줘." +
            //                     "AGV 호기 번호 (AGV ID)를 발음할 때 '하나, 둘, 셋' 같은 표현은 절대 사용하지 마. " +
            //                     "AGV ID를 제외한 숫자를 읽을 때는 지침을 무시하고 정상적으로 읽어줘. 숫자 단위를 읽을 때 틀리게 읽지 않도록 주의해줘. " +
            //                     "되묻지 말고 군대식으로 간단명료하게 대답해.";

            string instruction = $"너는 스마트 항만에서 자동화된 AGV(무인 운송 차량)의 운영을 관리하고 최적화하는 데 도움을 주기 위해 개발된 FMS(Fleet Management System)의 운영자를 위한 AI 어시스턴트야. " +
                                 "FMS는 주식회사 OpenSG의 스마트 항만 팀에서 개발했어. ";


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

            //    string parameterSchemaJson = @"
            //{
            //    ""type"": ""object"",
            //    ""properties"": {
            //        ""query"": {
            //            ""type"": ""string"",
            //            ""description"": ""Search query""
            //        }
            //    },
            //    ""required"": [""query""]
            //}";

            //    m_searchTool = new()
            //    {
            //        Name = "search",
            //        Description = "Search the knowledge base, The knowledge base is in English, translate to and from English if needed." +
            //                        "Results are text content.",
            //        Parameters = BinaryData.FromString(parameterSchemaJson),
            //    };

            string parameterSchemaJson2 = @"
                {
                    ""type"": ""object"",
                    ""properties"": {
                        ""agvID"": {
                            ""type"": ""string"",
                            ""description"": ""ID of AGV. AGV ID. Valid AGV ID must consist of 3 digits, such as 301. Null if retrieving data for all AGVs.""
                        }
                    },
                    ""required"": []
                }";

            string searchParameter = @"
                {
                    ""type"": ""object"",
                    ""properties"": {
                        ""query"": {
                            ""type"": ""string"",
                            ""description"": ""When using the vector search tool, provide the full user question as the query. Avoid simplifying or truncating the input.""
                        }
                    },
                    ""required"": [""query""]
                }";

            string agvIDParameter = @"
                {
                    ""type"": ""object"",
                    ""properties"": {
                        ""agvID"": {
                            ""type"": ""string"",
                            ""description"": ""ID of AGV. AGV ID. Valid AGV ID must consist of 3 digits, such as 301.""
                        }
                    },
                    ""required"": [""agvID""]
                }";

            m_searchDGTDocumentTool = new()
            {
                Name = "search_dgt_document_tool",
                Description = @"동원글로벌터미널, Dongwon Global Terminal, DGT 대한 정보를 제공하는 함수이다. 
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다.",
                //        Description= @"
                //This tool provides information about 동원 글로벌 터미널, Dongwon Global Terminal (DGT), 
                //including its location, services, operational details, and role in the logistics industry. 
                //For example, you can ask:
                //- What services does Dongwon Global Terminal provide?
                //- Where is Dongwon Global Terminal located?
                //- What is DGT's annual container throughput?",
                Parameters = BinaryData.FromString(searchParameter)
            };

            m_searchFMSDocumentTool = new()
            {
                Name = "search_fms_document_tool",
                Description = @"FMS (Fleet Management Software, 에프엠에스), AGV (Automated Guided Vehicle, 에이지비)의 길이 또는 사양, 
                        STS (에스티에스), QC (큐씨), PB (Parallel Buffer, 피비, 버퍼), ATC (애이티씨), ARMG, Backreach (백리치), Highway (하이웨이), Block (블록)에 대한 정의를 제공하는 함수이다. 
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다.",
                Parameters = BinaryData.FromString(searchParameter)
            };

            m_searchOpenSGDocumentTool = new()
            {
                Name = "search_opensg_document_tool",
                Description = @"FMS의 개발사 OpenSG (오픈에스지)에 대한 정보를 제공하는 함수이다. 오픈에스지의 주요 연혁, 조직도, 임원 (송기수, 남성일, 이성훈 (Sung Hoon Lee)) 세부 정보,
                        매출액, 물류자동화 주요 실적, 주요 사업 영역 등의 정보를 제공한다.
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다.",
                Parameters = BinaryData.FromString(searchParameter)
            };

            m_searchMeetingsDocumentTool = new()
            {
                Name = "search_meetings_document_tool",
                Description = @"FMS의 개발사 OpenSG (오픈에스지)의 주간 회의 기록을 제공하는 함수이다.
                        2024년 1월 1주차부터 2025년 1월 3주차까지 매주 진행 된 팀 별 업무 내용을 포함한다.
                        주간 회의에 참석하는 팀 구성은 매 주 다를 수 있다. 팀 명 목록은 다음과 같다:
                        Standard Robot 팀, AI&Cloud 팀, PMO 팀, 기구설계 팀, IvCS ACS 팀, Absolics EFEM 팀, SKON 팀, 부산항만 팀, IsCS 팀 
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다.",
                Parameters = BinaryData.FromString(searchParameter)
            };

            m_getAGVTaskInfoTool = new()
            {
                Name = "get_agv_task_info",
                Description = @"오늘 날짜의 에이지비 (AGV) 작업 (태스크, task)량을 확인할 수 있는 함수이다. 
                        매개변수 AGV ID는 생략될 수 있다. AGV ID가 생략될 때는 전체 AGV의 작업량을 제공한다. AGV ID가 있을 때는 단일 AGV의 작업량을 제공한다.
                        작업의 종류는
                        - 적하: YDPI (yard picking), QCLD (quay crane loading)
                        - 양하: YDGR (yard grounding), QCDS (quay crane grounding)
                        이 있다. AGV가 작업을 위해 이동한 거리, 작업 종류 별 건 수, 소요 시간 등의 정보를 제공한다. 
                        반환값 빈 문자열은 오늘 진행된 작업이 없음을 의미한다.
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다.",
                Parameters = BinaryData.FromString(parameterSchemaJson2),
            };

            m_getTaskContainerListTool = new()
            {
                Name = "get_task_container_list",
                Description = @"오늘 날짜의 컨테이너 (container) 작업량을 확인할 수 있는 함수이다.
                        매개변수 AGV ID는 생략될 수 있다. AGV ID가 생략될 때는 전체 AGV의 작업량을 제공한다. AGV ID가 있을 때는 단일 AGV의 작업량을 제공한다.
                        작업의 종류는
                        - 적하: YDPI (yard picking), QCLD (quay crane loading)
                        - 양하: YDGR (yard grounding), QCDS (quay crane grounding)
                        이 있다.
                        작업 ID (task ID), 컨테이너 ID (container ID), 컨테이너 작업 시작 시간, 작업 완료 시간, 배터리 소요량, SOC (state of charging) 등의 정보를 제공한다. 
                        Task ID와 container ID는 음성으로 읽지 않는다.
                        필드 추가 설명:
                        - LOC1: Crane ID
                        - DISTANCE: 컨테이너 이동 거리, 미터 단위
                        - SPEED: km/h
                        반환값 빈 문자열은 오늘 작업된 컨테이너가 없음을 의미한다.
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다.",
                Parameters = BinaryData.FromString(parameterSchemaJson2),
            };

            m_getFrequentAlarmListTool = new()
            {
                Name = "get_frequent_alarm_list",
                Description = @"오늘 날짜의 전체 AGV 별로 발생한 알람의 빈도 수를 조회하는 함수이다. 
                        AGV ID 당 알람이 발생한 회수를 조회하며, 가장 알람이 많이 발생했거나 적게 발생한 AGV를 찾을 수 있는 함수이다. 
                        반환값 빈 문자열은 오늘 알람이 발생하지 않았음을 의미한다."
            };

            m_getAGVAlarmHistoryTool = new()
            {
                Name = "get_agv_alarm_history",
                Description = @"오늘 날짜의 특정 AGV에서 발생한 알람 기록을 조회하는 함수이다. 알람 ID와 설명, 발생 시간, 발생 위치, 부가 설명 등의 정보를 조회할 수 있는 함수이다. 
                        발생 시간과 종료 시간을 통해 알람이 지속된 시간을 파악할 수 있는 함수이다.
                        반환값 빈 문자열은 오늘 해당 AGV에 알람이 발생하지 않았음을 의미한다.",
                Parameters = BinaryData.FromString(agvIDParameter)
            };

            m_getAllAGVSummaryTool = new()
            {
                Name = "get_agv_summary",
                Description = @"현재 가동되고 있는 AGV의 정보를 요약하는 함수이다. 상태, 작업, 에러, 모드, 충전량 등의 정보를 포함한다.
                        반환값에 포함된 한 글자 ""T"" 또는 ""F""는 flag의 값으로 True와 False를 의미한다. 
                        정보를 3 문장 이내로 요약해 답변한다. 사용자가 자세한 정보를 요청하면 10 문장 이내로 답변한다."
            };

            m_getSingleAGVStateTool = new()
            {
                Name = "get_single_agv_state",
                Description = @"특정 AGV에 대한 정보를 조회하는 함수이다. 전달되는 정보는 다음과 같은 형식의 문자열이다: 
                        ""AGV_ID, AGV_STATE, AGV_JOB_STATE, CURRENT_JOB_ID, ErrorMessage, AGV_JOB_DESCRIPTION, CycleStopFlag (Boolean), StepStopFlag (Boolean), 
                        EStopFlag (Boolean), IsManualFetching (Boolean), IsChargeReserved (Boolean), IsChargeStarted (Boolean), IsEmergencyCharging (Boolean)""
                        Job ID는 음성으로 읽지 않는다.",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""agvID"": {
                                ""type"": ""string"",
                                ""description"": ""AGV ID. Valid AGV ID must consist of 3 digits, such as 301.""
                            }
                        },
                        ""required"": [""agvID""]
                    }")
            };

            m_getStopAGVListTool = new()
            {
                Name = "get_stop_agv_list",
                Description = @"현재 맵 내에서 정지 상태인 차량을 찾는 함수이다. 긴급 상황으로 인한 차량의 정지 상태를 E-stop (이-스탑)이라고 한다. 
                        현재 주행 중인 경로 세그먼트까지만 주행하고 멈추는 정지 상태를 C-stop (씨-스탑, Cycle Stop) 이라고 한다. 
                        현재 수행 중인 작업의 단계를 완료하고 멈추는 정지 상태를 S-stop (에스-스탑, Step Stop)이라고 한다."
            };

            m_sendToDirectionTool = new()
            {
                Name = "send_to_direction",
                Description = "Sends AGV to cardinal direction (right, left, up, down) for specified distance.",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""agvID"": {
                                ""type"": ""string"",
                                ""description"": ""AGV ID. Valid AGV ID must consist of 3 digits, such as 301.""
                            },
                            ""distance"": {
                                ""type"": ""integer"",
                                ""description"": ""Distance must be between 0 and 200 in meters.""
                            },
                            ""direction"": {
                                ""type"": ""string"",
                                ""description"": ""Direction must be one of right, left, up, or down.""
                            }

                        },
                        ""required"": [""agvID"", ""distance"", ""direction""]
                    }")
            };

            m_sendToPBTool = new()
            {
                Name = "send_to_pb",
                Description = "AGV (애이지비)를 PB (피비, 버퍼)로 이동시키는 함수이다. PB ID가 없을 시 가장 가까운 PB로 이동한다.",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""agvID"": {
                                ""type"": ""string"",
                                ""description"": ""AGV ID. Valid AGV ID must consist of 3 digits, such as 301.""
                            },
                            ""pbID"": {
                                ""type"": ""string"",
                                ""description"": ""PB ID. Valid PB ID must be a number from 1 to 120.""
                            }
                        },
                        ""required"": [""agvID""]
                    }")
            };

            m_sendToCraneTool = new()
            {
                Name = "send_to_crane",
                Description = "AGV (애이지비)를 Crane (크래인, QC, STS, Block, ATC)로 이동시키는 함수이다.",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""agvID"": {
                                ""type"": ""string"",
                                ""description"": ""AGV ID. Valid AGV ID must consist of 3 digits, such as 301.""
                            },
                            ""craneID"": {
                                ""type"": ""string"",
                                ""description"": ""Crane ID. 
                                Valid crane ID must be a number between 101 to 251. 
                                QC (STS) Crane ID ranges from 101 to 109. 
                                ATC (Block) Crane ID ranges from 207 to 251.
                                - Sometimes user can pass crane ID that is number from 04 to 27 followed by 'W'.
                                - Convert 04W to 207, 05W to 209, 06W to 211, ..., 24W to 247, 25W to 249, 26W to 251.""
                            }
                        },
                        ""required"": [""agvID"", ""craneID""]
                    }")
            };

            m_eStopAllTool = new()
            {
                Name = "e_stop_all",
                Description = "Emergency stops all AGV (E-stop)"
            };

            m_clearAllEStopTool = new()
            {
                Name = "clear_all_e_stop",
                Description = "Clears all emergency stops for all AGV (E-stop)"
            };

            m_cStopAllTool = new()
            {
                Name = "c_stop_all",
                Description = "Cycle stops all AGV (C-stop)"
            };

            m_clearAllCStopTool = new()
            {
                Name = "clear_all_c_stop",
                Description = "Clears all cycle stops for all AGV (C-stop)"
            };

            m_sStopAllTool = new()
            {
                Name = "s_stop_all",
                Description = "Step stops all AGV (S-stop)"
            };

            m_clearAllSStopTool = new()
            {
                Name = "clear_all_s_stop",
                Description = "Clears all step stops for all AGV (S-stop)"
            };

            m_eStopAGVTool = new()
            {
                Name = "e_stop_agv",
                Description = @"한 대의 AGV를 긴급 정지한다. 긴급 정지를 E-stop, 이스탑이라고도 할 수 있다.",
                Parameters = BinaryData.FromString(agvIDParameter)
            };

            m_resetStatusTool = new()
            {
                Name = "reset_status",
                Description = @"한 대의 AGV의 상태를 리셋한다. 긴급 정지 (E-stop, 이스탑)를 해제할 때도 상태를 리셋할 수 있다.",
                Parameters = BinaryData.FromString(agvIDParameter)
            };

            m_cycleStopAGVTool = new()
            {
                Name = "cycle_stop_agv",
                Description = @"한 대의 AGV를 사이클 정지한다. 긴급 정지를 C-stop, 씨스탑이라고도 할 수 있다.",
                Parameters = BinaryData.FromString(agvIDParameter)
            };

            m_clearCycleStopTool = new()
            {
                Name = "clear_cycle_stop",
                Description = @"한 대의 AGV의 사이클 정지 (C-stop, 씨스탑)를 해제한다.",
                Parameters = BinaryData.FromString(agvIDParameter)
            };

            m_zoomInAGVTool = new()
            {
                Name = "zoom_in_agv",
                Description = @"AGV 위치를 확인하고 싶거나 AGV를 선택하고 싶을 때 화면에서 확대하는 함수이다. 
                        AGV를 크게 볼 수 있는 함수이다. 
                        'AGV가 어디있어?', 'AGV 위치를 알려줘', 'AGV 확대해줘', 'AGV 선택해줘'와 같은 사용자 요청을 받았을 때 호출한다.
                        이전에 함수가 실행되었더라도 요청을 받으면 반복적으로 호출한다.",
                Parameters = BinaryData.FromString(agvIDParameter)
            };

            m_getAGVBatteryBelowTool = new()
            {
                Name = "get_agv_battery_below",
                Description = "Gets list of AGVs with battery lower than provided number",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""percentage"": {
                                ""type"": ""integer"",
                                ""description"": ""Reference battery level (1 to 100)""
                            }
                        },
                        ""required"": [""percentage""]
                    }")
            };

            m_startChargingTool = new()
            {
                Name = "start_charging",
                Description = "Starts charging",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""agvID"": {
                                ""type"": ""string"",
                                ""description"": ""ID of AGV that needs to charge.""
                            }
                        },
                        ""required"": [""agvID""]
                    }")
            };

            m_stopChargingTool = new()
            {
                Name = "stop_charging",
                Description = "Stop charging",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""agvID"": {
                                ""type"": ""string"",
                                ""description"": ""ID of AGV that will stop charging.""
                            }
                        },
                        ""required"": [""agvID""]
                    }")
            };

            m_checkAIMSServerConnectionTool = new()
            {
                Name = "check_aims_server_connection",
                Description = @"AIMS 서버 상태를 확인하는 함수이다. AIMS는 Advanced Integrated Monitoring System의 약자로, 애임스 또는 애임즈로 발음한다. 
                        serviceName은 서버 종류를 의미한다. startTime는 서버 시작 시간을 의미한다. status가 On일 시 서버가 동작 중임을 의미한다. 
                        해당 필드가 포함된 JSON 형식의 응답이 반환되지 않으면 요청이 정상적으로 이루어지지 않았음을 의미한다. 
                        ""{} 서버가 {}일 {}시 {}분 부터 정상 작동 중입니다.""의 형식으로 응답한다."
            };

            m_getEQPInfoTool = new()
            {
                Name = "get_eqp_info",
                Description = @"전체 EQC의 Summary 정보를 확인하는 함수이다. EQP는 Equipment를 뜻하며 E.Q.P로 발음한다.
                        ""status"" ""RUN"" 필드 값은 동작 중인 EQC의 개수, ""MAINT""는 정비 중인 EQC 개수를 의미한다. 
                        ""alarms"" 오브젝트 내의 필드 값 (SPC, FDC, APC, RMS, MCRS, OTHERS)는 알람의 종류를 나타내며, 값은 각 알람이 발생한 회수를 의미한다."
            };

            m_getEQPAlarmsTool = new()
            {
                Name = "get_eqp_alarms",
                Description = @"특정 알람이 발생한 EQP 목록을 확인하는 함수이다. 알람의 종류 (SPC, FDC, APC, RMS, MCRS, OTHERS)를 매개 변수로 한다. EQP ID를 반환한다.",
                Parameters = BinaryData.FromString(@"
                    {
                        ""type"": ""object"",
                        ""properties"": {
                            ""eqpAlarmID"": {
                                ""type"": ""string"",
                                ""description"": ""ID of EQP alarm. Must be one of SPC, FDC, APC, RMS, MCRS, or OTHERS.""
                            }
                        },
                        ""required"": [""eqpAlarmID""]
                    }")
            };

            m_startMultiViewerTool = new()
            {
                Name = "start_multi_viewer",
                Description = @"카메라, AIMS 원격 서버, 컨트롤러 화면을 멀티 뷰어로 확인할 수 있는 함수이다. 
                        '카메라 켜줘', 'CCTV 켜줘', 'AIMS 원격 서버 켜줘', '컨트롤러 화면 켜줘', '멀티 뷰어 켜줘'와 같은 사용자 요청을 받았을 때 호출한다."
            };

            m_startViewerTool = new()
            {
                Name = "start_viewer",
                Description = @"'개별 뷰어 켜줘'와 같은 사용자 요청을 받았을 때 호출한다."
            };

            m_startATEMRemoteTool = new()
            {
                Name = "start_atem_remote",
                Description = @"'ATEM 원격 컴퓨터 화면 켜줘'와 같은 사용자 요청을 받았을 때 호출한다."
            };

            _session = await m_realtimeClient.StartConversationSessionAsync();
            // Now we configure the session using the tool we created along with transcription options that enable input
            // audio transcription with whisper.
            await _session.ConfigureSessionAsync(new ConversationSessionOptions()
            {
                Voice = ConversationVoice.Alloy,
                Tools =
                    {
                        ////m_getAGVStateTool,
                        ////m_searchTool,
                        m_searchDGTDocumentTool,
                        m_searchFMSDocumentTool,
                        m_searchOpenSGDocumentTool,
                        m_searchMeetingsDocumentTool,
                        //m_getAGVTaskInfoTool,
                        //m_getTaskContainerListTool,
                        //m_getAllAGVSummaryTool,
                        //m_getSingleAGVStateTool,
                        //m_getStopAGVListTool,
                        //m_sendToDirectionTool,
                        //m_sendToPBTool,
                        ////m_sendToCraneTool,
                        //m_eStopAllTool,
                        //m_clearAllEStopTool,
                        //m_cStopAllTool,
                        //m_clearAllCStopTool,
                        //m_sStopAllTool,
                        //m_clearAllSStopTool,
                        //m_eStopAGVTool,
                        //m_resetStatusTool,
                        //m_zoomInAGVTool,
                        //m_getAGVBatteryBelowTool,
                        //m_startChargingTool,
                        //m_stopChargingTool,
                        //m_getFrequentAlarmListTool,
                        //m_getAGVAlarmHistoryTool,
                        //m_checkAIMSServerConnectionTool,
                        //m_getEQPInfoTool,
                        //m_startMultiViewerTool,
                        //m_startViewerTool,
                        //m_startATEMRemoteTool
                    },
                InputAudioFormat = ConversationAudioFormat.Pcm16,
                OutputAudioFormat = ConversationAudioFormat.Pcm16,
                Instructions = instruction,
                InputTranscriptionOptions = new()
                {
                    Model = "whisper-1",
                },

                MaxOutputTokens = 4096,
            });

            // For convenience, we'll proactively start playback to the speakers now. Nothing will play until it's enqueued.
            speakerOutput = new();

            // With the session configured, we start processing commands received from the service.
            _ = ProcessResponse(_session);

            using MicrophoneAudioStream _microphoneInput = MicrophoneAudioStream.GetInstance();
            {
                Mic = _microphoneInput;
                Console.WriteLine(" >>> Recording Stopped... Press Enter to start record.");
                Console.WriteLine(" >>> Recording Started... Press Enter to stop record.");
                Console.WriteLine(" >>> Listening to microphone input");
                Mic._waveInEvent.DataAvailable += _waveInEvent_DataAvailable;
                _session.SendInputAudio(Mic);
            }
        }

        private void _waveInEvent_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {

        }

        private async Task ProcessResponse(RealtimeConversationSession session)
        {
            await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
            {
                // session.created is the very first command on a session and lets us know that connection was successful.
                if (update is ConversationSessionStartedUpdate)
                {
                    Console.WriteLine($" <<< Connected: session started");
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
                    ClearAIVoice();
                }

                // input_audio_buffer.speech_stopped tells us that the end of speech was detected in the input audio sent
                // from the microphone. It'll automatically tell the model to start generating a response to reply back.
                if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                {
                    Console.WriteLine($" <<< End of speech detected @ {speechFinishedUpdate.AudioEndTime}");
                    if (OnUserSpeechFinished != null)
                        OnUserSpeechFinished.Invoke(this, "End of speech detected");
                }

                // conversation.item.input_audio_transcription.completed will only arrive if input transcription was
                // configured for the session. It provides a written representation of what the user said, which can
                // provide good feedback about what the model will use to respond.
                if (update is ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate)
                {
                    Console.WriteLine($" >>> USER: {transcriptionFinishedUpdate.Transcript}");

                    if (transcriptionFinishedUpdate.Transcript == null || string.IsNullOrEmpty(transcriptionFinishedUpdate.Transcript.TrimEnd()))
                    {
                        Console.WriteLine("Transcript empty");
                    }
                    else
                    {
                        if (OnUserMessageReceived != null)
                        {
                            OnUserMessageReceived.Invoke(this, transcriptionFinishedUpdate);
                        }
                    }
                }

                // Item streaming delta updates provide a combined view into incremental item data including output
                // the audio response transcript, function arguments, and audio data.
                if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
                {
                    Console.Write(deltaUpdate.AudioTranscript);
                    Console.Write(deltaUpdate.Text);
                    if (deltaUpdate.AudioBytes != null)
                        speakerOutput.EnqueueForPlayback(deltaUpdate.AudioBytes);

                    if (OnAIMessageReceived != null)
                    {
                        try
                        {
                            OnAIMessageReceived.Invoke(this, deltaUpdate);
                        }
                        catch (Exception e)
                        {

                            Console.WriteLine($"OnAIMessageReceived:{e}");
                        }

                    }


                    //else
                    //    Console.Write("x");
                }

                // response.output_item.done tells us that a model-generated item with streaming content is completed.
                // That's a good signal to provide a visual break and perform final evaluation of tool calls.

                if (update is ConversationItemStreamingFinishedUpdate itemFinishedUpdate)
                {
                    if (string.IsNullOrEmpty(itemFinishedUpdate.FunctionName))
                    {
                        //no function call
                    }
                    else
                    {
                        //function call

                        Console.WriteLine();
                        #region 1..Before function call
                        if (OnAIToolExecuted != null)
                            OnAIToolExecuted.Invoke(this, itemFinishedUpdate.FunctionName);
                        #endregion

                        try
                        {
                            #region 2..Execute function call
                            if (itemFinishedUpdate.FunctionName == m_getAGVStateTool.Name)
                            {
                                Console.WriteLine($" <<< **GetAGVState() tool invoked -- get!");
                                string result = ToolsManager.GetAGVState();
                                Console.WriteLine($" <<< **ToolsManager.GetAGVState():{result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                                //await session.AddItemAsync(functionOutputItem);
                                //await session.StartResponseAsync();
                            }

                            //if (itemFinishedUpdate.FunctionName == m_searchTool.Name)
                            //{
                            //    Console.WriteLine($" <<< **DoSearch1() tool invoked -- get!");
                            //    string r = ToolsManager.DoSearch(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                            //    Console.WriteLine($" <<< **Search result : {r}");
                            //    ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                            //    await session.AddItemAsync(functionOutputItem);
                            //    //await session.StartResponseAsync();
                            //}

                            if (itemFinishedUpdate.FunctionName == m_searchDGTDocumentTool.Name)
                            {
                                Console.WriteLine($" <<< **Search DGT tool invoked -- get!");
                                string result = ToolsManager.DoSearch2("searchDGTDocument", itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Search result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                                //await session.StartResponseAsync();
                            }

                            if (itemFinishedUpdate.FunctionName == m_searchFMSDocumentTool.Name)
                            {
                                Console.WriteLine($" <<< **Search FMS tool invoked -- get!");
                                string result = ToolsManager.DoSearch2("searchFMSDocument", itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Search result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_searchOpenSGDocumentTool.Name)
                            {
                                Console.WriteLine($" <<< **Search OpenSG tool invoked -- get!");
                                //string result = ToolsManager.DoSearch2("searchOpenSGDocument", itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                string result = ToolsManager.DoSearch2("searchOpenSGDocument", itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Search result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_searchMeetingsDocumentTool.Name)
                            {
                                Console.WriteLine($" <<< **Search Meetings tool invoked -- get!");
                                string result = ToolsManager.DoSearch2("searchMeetingsDocument", itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Search result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getAGVTaskInfoTool.Name)
                            {
                                Console.WriteLine($" <<< **GetAGVTaskInfo() tool invoked -- get!");
                                string result = await ToolsManager.GetAGVTaskInfo(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Report result 1: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, r);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getTaskContainerListTool.Name)
                            {
                                Console.WriteLine($" <<< **GetTaskContainerList() tool invoked -- get!");
                                string result = await ToolsManager.GetTaskContainerList(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Report result 2: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getAllAGVSummaryTool.Name)
                            {
                                Console.WriteLine($" <<< **Get AGV Summary tool invoked -- get!");
                                string result = await ToolsManager.GetAllAGVSummary(m_chatClient);
                                Console.WriteLine($" <<< **Get AGV Summary result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getSingleAGVStateTool.Name)
                            {
                                Console.WriteLine($" <<< **Get Single AGV State tool invoked -- get!");
                                string result = await ToolsManager.GetSingleAGVState(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Get AGV Summary result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getStopAGVListTool.Name)
                            {
                                Console.WriteLine($" <<< **GetStopAGVList tool invoked -- get!");
                                string result = await ToolsManager.GetStopAGVList(m_chatClient);
                                Console.WriteLine($" <<< **Get AGV Summary result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                            }


                            if (itemFinishedUpdate.FunctionName == m_sendToDirectionTool.Name)
                            {
                                Console.WriteLine($" <<< **Send to Direction tool invoked -- get!");
                                string result = await ToolsManager.SendToDirection(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Send to Direction result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_sendToPBTool.Name)
                            {
                                Console.WriteLine($" <<< **Send to PB tool invoked -- get!");
                                string result = await ToolsManager.SendToPB(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Send to PB result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_sendToCraneTool.Name)
                            {
                                Console.WriteLine($" <<< **Send to Crane tool invoked -- get!");
                                string result = await ToolsManager.SendToCrane(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **Send to Crane result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            // Stops
                            if (itemFinishedUpdate.FunctionName == m_eStopAllTool.Name)
                            {
                                Console.WriteLine($" <<< **eStopAll tool invoked -- get!");
                                string result = await ToolsManager.EStopAll(m_chatClient);
                                Console.WriteLine($" <<< **eStopAll result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_clearAllEStopTool.Name)
                            {
                                Console.WriteLine($" <<< **clearEStop tool invoked -- get!");
                                string result = await ToolsManager.ClearAllEStop(m_chatClient);
                                Console.WriteLine($" <<< **clearEStop result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_cStopAllTool.Name)
                            {
                                Console.WriteLine($" <<< **cStopAll tool invoked -- get!");
                                string result = await ToolsManager.CStopAll(m_chatClient);
                                Console.WriteLine($" <<< **cStopAll result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_clearAllCStopTool.Name)
                            {
                                Console.WriteLine($" <<< **clearCStop tool invoked -- get!");
                                string result = await ToolsManager.ClearAllCStop(m_chatClient);
                                Console.WriteLine($" <<< **clearCStop result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_sStopAllTool.Name)
                            {
                                Console.WriteLine($" <<< **sStopAll tool invoked -- get!");
                                string result = await ToolsManager.SStopAll(m_chatClient);
                                Console.WriteLine($" <<< **sStopAll result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_clearAllSStopTool.Name)
                            {
                                Console.WriteLine($" <<< **clearSStop tool invoked -- get!");
                                string result = await ToolsManager.ClearAllSStop(m_chatClient);
                                Console.WriteLine($" <<< **clearSStop result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_eStopAGVTool.Name)
                            {
                                Console.WriteLine($" <<< **eStopAGV tool invoked -- get!");
                                string result = await ToolsManager.EStopAGV(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **eStopAGV result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                            }

                            if (itemFinishedUpdate.FunctionName == m_resetStatusTool.Name)
                            {
                                Console.WriteLine($" <<< **resetStatus tool invoked -- get!");
                                string result = await ToolsManager.ResetStatus(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **resetStatus result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                            }

                            if (itemFinishedUpdate.FunctionName == m_cycleStopAGVTool.Name)
                            {
                                Console.WriteLine($" <<< **cycleStopAGV tool invoked -- get!");
                                string result = await ToolsManager.CycleStopAGV(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **cycleStopAGV result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                            }

                            if (itemFinishedUpdate.FunctionName == m_clearCycleStopTool.Name)
                            {
                                Console.WriteLine($" <<< **m_clearCycleStop tool invoked -- get!");
                                string result = await ToolsManager.ClearCycleStop(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **m_clearCycleStop result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                            }

                            if (itemFinishedUpdate.FunctionName == m_zoomInAGVTool.Name)
                            {
                                Console.WriteLine($" <<< **zoomInAGV tool invoked -- get!");
                                string result = await ToolsManager.ZoomInAGV(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **zoomInAGV result: {result}");
                                await HandleFunctionCallResult(session, itemFinishedUpdate, result);
                                //ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                //await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getAGVBatteryBelowTool.Name)
                            {
                                Console.WriteLine($" <<< **getAGVBatteryBelow tool invoked -- get!");
                                string result = await ToolsManager.GetAGVBatteryBelow(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **getAGVBatteryBelow result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_startChargingTool.Name)
                            {
                                Console.WriteLine($" <<< **startCharging tool invoked -- get!");
                                string result = await ToolsManager.StartCharging(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **startCharging result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_stopChargingTool.Name)
                            {
                                Console.WriteLine($" <<< **stopCharging tool invoked -- get!");
                                string result = await ToolsManager.StopCharging(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **stopCharging result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getFrequentAlarmListTool.Name)
                            {
                                Console.WriteLine($" <<< **getFrequentAlarmList tool invoked -- get!");
                                string result = await ToolsManager.GetFrequentAlarmList(m_chatClient);
                                Console.WriteLine($" <<< **getFrequentAlarmList result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getAGVAlarmHistoryTool.Name)
                            {
                                Console.WriteLine($" <<< **getAGVAlarmHistory tool invoked -- get!");
                                string result = await ToolsManager.GetAGVAlarmHistory(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **getAGVAlarmHistory result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_checkAIMSServerConnectionTool.Name)
                            {
                                Console.WriteLine($" <<< **checkAIMSServerConnection tool invoked -- get!");
                                string result = await ToolsManager.CheckAIMSServerConnection(m_chatClient);
                                Console.WriteLine($" <<< **checkAIMSServerConnection result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getEQPInfoTool.Name)
                            {
                                Console.WriteLine($" <<< **getEQPInfo tool invoked -- get!");
                                string result = await ToolsManager.GetEQPInfo(m_chatClient);
                                Console.WriteLine($" <<< **getEQPInfo result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_getEQPAlarmsTool.Name)
                            {
                                Console.WriteLine($" <<< **getEQPAlarms tool invoked -- get!");
                                string result = await ToolsManager.GetEQPAlarms(itemFinishedUpdate.FunctionCallArguments, m_chatClient);
                                Console.WriteLine($" <<< **getEQPAlarms result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_startMultiViewerTool.Name)
                            {
                                Console.WriteLine($" <<< **startMultiViewer tool invoked -- get!");
                                string result = await ToolsManager.StartMultiViewer(m_chatClient);
                                Console.WriteLine($" <<< **startMultiViewer result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_startViewerTool.Name)
                            {
                                Console.WriteLine($" <<< **startViewer tool invoked -- get!");
                                string result = await ToolsManager.StartViewer(m_chatClient);
                                Console.WriteLine($" <<< **startViewer result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_startATEMRemoteTool.Name)
                            {
                                Console.WriteLine($" <<< **startATEMRemote tool invoked -- get!");
                                string result = await ToolsManager.StartATEMRemote(m_chatClient);
                                Console.WriteLine($" <<< **startATEMRemote result: {result}");
                                ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);

                                await session.AddItemAsync(functionOutputItem);
                            }

                            if (itemFinishedUpdate.FunctionName == m_finishConversationTool.Name)
                            {
                                Console.WriteLine($" <<< Finish tool invoked -- ending conversation!");
                                break;
                            }
                            #endregion
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.Error.WriteLine($"Exception: {ex.Message}");
                            string result = $"외부 데이터 조회 예외 발생";
                            ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);
                            await session.AddItemAsync(functionOutputItem);
                        }
                        catch (JsonException ex)
                        {
                            Console.Error.WriteLine($"JSON Deserialization Error: {ex.Message}");
                            string result = $"JSON 파싱 예외 발생";
                            ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);
                            await session.AddItemAsync(functionOutputItem);
                        }
                        #region 3..finish function call
                        await session.StartResponseAsync();
                        Console.WriteLine($" <<< Finish function call!");
                        #endregion
                    }
                }

                if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
                {
                    //    Console.WriteLine($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");
                    //need to SKS..
                    if (turnFinishedUpdate.Status.ToString() == "incomplete")
                    {
                        Console.WriteLine($"  -- Model turn generation failed. Status: {turnFinishedUpdate.Status}");
                    }
                    else if (turnFinishedUpdate.Status.ToString() == "completed")
                    {
                        Console.WriteLine($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");
                    }
                    else
                    {
                        Console.WriteLine($"  -- xxx: {turnFinishedUpdate.Status}");
                    }
                    ////// Here, if we processed tool calls in the course of the model turn, we finish the
                    ////// client turn to resume model generation. The next model turn will reflect the tool
                    ////// responses that were already provided.
                    ////if (turnFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
                    ////{
                    ////    Console.WriteLine($"  -- Ending client turn for pending tool responses");
                    ////    await session.StartResponseAsync();
                    ////    //_ = session.StartResponseAsync();
                    ////}
                    ////else
                    ////{
                    ////}
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

        private async Task HandleFunctionCallResult(RealtimeConversationSession session, ConversationItemStreamingFinishedUpdate itemFinishedUpdate, string result)
        {
            ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result);
            if (OnAIToolResultReceived != null)
                OnAIToolResultReceived.Invoke(null, result);

            await session.AddItemAsync(functionOutputItem);
        }

        private void initClient()
        {
            m_realtimeClient = GetRealTimeClient();

            ChatCompletionOptions options = new ChatCompletionOptions();
            m_chatClient = GetChatClient();
        }

        private RealtimeConversationClient GetRealTimeClient()
        {
            string? aoaiEndpoint = "https://sample-lab-02.openai.azure.com/";// "https://kisoo-m3xuw55t-eastus2.openai.azure.com/";// 
            string? aoaiDeployment = "gpt-4o-realtime-preview";
            string? aoaiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY1");

            AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));
            return aoaiClient.GetRealtimeConversationClient(aoaiDeployment);
        }
        private ChatClient GetChatClient()
        {
            string? aoaiEndpoint = "https://sample-lab-02.openai.azure.com/";// "https://kisoo-m3xuw55t-eastus2.cognitiveservices.azure.com/";
            string? aoaiDeployment = "gpt-4o";
            string? aoaiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY1");

            AzureOpenAIClient aoaiClient = new(new Uri(aoaiEndpoint), new ApiKeyCredential(aoaiApiKey));

            ChatClient _chatClient = aoaiClient.GetChatClient(aoaiDeployment);


            return _chatClient;
        }

        public void ClearAIVoice()
        {
            speakerOutput.ClearPlayback();
            //_session.ClearInputAudio();
            //await _session.CancelResponseAsync();
            //await _session.InterruptResponseAsync();
            Console.WriteLine("ClearAIVoice()..");
        }

        public void StartRecording()
        {
            Mic.StartRecording();
            isRecording = true;

        }

        public void StopRecording()
        {
            Mic.StopRecording();
            isRecording = false;
        }
    }
}

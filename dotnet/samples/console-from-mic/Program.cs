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
using OpenSG.AI;

#pragma warning disable OPENAI002
#pragma warning disable AOAI001 

public class Program
{
    public static async Task Main(string[] args)
    {
        OpenSGManager agent=new OpenSGManager();
        await agent.RunAIAgent();

        Console.WriteLine("app end...");
    }
}


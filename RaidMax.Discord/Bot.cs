using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidMax.DiscordBot
{
    class Bot
    {
        static void Main(string[] args)
        {
            string token = Environment.GetEnvironmentVariable("discord-bot-token");
            var Client = new Main.DiscordClient();
            Task.FromResult(Client.Initialize(token));

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}

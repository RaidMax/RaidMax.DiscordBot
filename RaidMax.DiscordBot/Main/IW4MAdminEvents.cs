using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace RaidMax.DiscordBot.Main
{
    public class IW4MAdminEvents
    {

        public static Task<RestEvent> GetLastestEvent()
        {
            try
            {
                var wc = new WebClient();

                return Task.Run(() =>
                {
                    string result = wc.DownloadString(new Uri("http://server.nbsclan.org:8080/api/events"));
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<EventResponse>(result).Event;
                });
            }

            catch (Exception)
            {
                return Task.Run(() =>
                {
                    return new RestEvent();
                });
            }
        }
        private struct EventResponse
        {
            public int eventCount { get; set; }
            public RestEvent Event { get; set; }
        }

        public struct RestEvent
        {
            public RestEvent(EventType Ty, EventVersion V, string M, string T, string O, string Ta)
            {
                Type = Ty;
                Version = V;
                Message = M;
                Title = T;
                Origin = O;
                Target = Ta;

                ID = Math.Abs(DateTime.Now.GetHashCode());
            }

            public enum EventType
            {
                NOTIFICATION,
                STATUS,
                ALERT,
            }

            public enum EventVersion
            {
                IW4MAdmin
            }

            public EventType Type { get; set; }
            public EventVersion Version { get; set; }
            public string Message { get; set; }
            public string Title { get; set; }
            public string Origin { get; set; }
            public string Target { get; set; }
            public int ID { get; set; }
        }
    }
}

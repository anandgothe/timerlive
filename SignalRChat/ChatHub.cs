using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
namespace SignalRTimer
{
    public class ChatHub : Hub
    {
        StorageOnAzureTable sessionTimouts = new StorageOnAzureTable(); // you can use any storage here, even an inmemory dictionary will do. I'm using Azure Table storage for persistance, so that we don't lose client timer values if the server restarts.
        
        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message + Context.ConnectionId);
            //Clients.we
        }

        public void RegisterClient(string sessionid)
        {
            var id = Context.ConnectionId;
            Groups.Add(id, sessionid); //this will add the connected user to particular group

            RefreshTimer(sessionid);
        }

        public void RefreshTimer(string sessionid)
        {
            bool isTimerOn = sessionTimouts.ContainsSession(sessionid);
            var session = sessionTimouts.GetSession(sessionid);

            if (isTimerOn)
            {
                int seconds = CalculateSeconds(session);
                Clients.Caller.sessionStarted(sessionid, true, seconds, session.Message ?? string.Empty);
            }
            else
            {
                Clients.Caller.sessionStarted(sessionid, false);
            }
        }

        public void StartTimer(string sessionid, string minutes, string message)
        {
            var whenEndsUtc = DateTime.UtcNow.AddMinutes(int.Parse(minutes));
            sessionTimouts.SetSession(sessionid, whenEndsUtc, message);
            BroadcastTimeLeft(sessionid);
        }

        private void BroadcastTimeLeft(string sessionid)
        {
            var session = sessionTimouts.GetSession(sessionid);
            int seconds = CalculateSeconds(session);

            string[] Exceptional = new string[0];
            Clients.Group(sessionid, Exceptional).startCountdown(seconds,session.Message);
        }

        private int CalculateSeconds(SessionEntity session)
        {
            if (session != null)
            {
                var howMuchLeft = session.EndsWhenUtc.Subtract(DateTime.UtcNow);
                var seconds = (int)howMuchLeft.TotalSeconds;
                return seconds;
            }

            return 0;
        }
    }
}
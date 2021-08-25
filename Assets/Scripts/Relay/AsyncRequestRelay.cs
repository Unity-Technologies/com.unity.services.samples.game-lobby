using System;
using Unity.Services.Relay;

namespace LobbyRelaySample.Relay
{
    public class AsyncRequestRelay : AsyncRequest
    {
        private static AsyncRequestRelay s_instance;
        public static AsyncRequestRelay Instance
        {
            get
            {   if (s_instance == null)
                    s_instance = new AsyncRequestRelay();
                return s_instance;
            }
        }

        protected override void ParseServiceException(Exception e)
        {
            // TODO: Implement
        }
    }
}

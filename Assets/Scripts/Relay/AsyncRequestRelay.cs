using System;
using Unity.Services.Relay;

namespace LobbyRelaySample.relay
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

        /// <summary>
        /// The Relay service will wrap HTTP errors in RelayServiceExceptions. We can filter on RelayServiceException.Reason for custom behavior.
        /// </summary>
        protected override void ParseServiceException(Exception e)
        {
            if (!(e is RelayServiceException))
                return;
            var relayEx = e as RelayServiceException;
            if (relayEx.Reason == RelayExceptionReason.Unknown)
                Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, "Relay Error: Relay service had an unknown error.");
            else
                Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, $"Relay Error: {relayEx.Message}");
        }
    }
}

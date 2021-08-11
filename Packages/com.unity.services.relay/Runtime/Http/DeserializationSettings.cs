using System;

namespace Unity.Services.Relay.Http
{   
    public enum MissingMemberHandling
    {
        Error,
        Ignore
    }
    public class DeserializationSettings
    {
        public MissingMemberHandling MissingMemberHandling = MissingMemberHandling.Error;
    }
    
}

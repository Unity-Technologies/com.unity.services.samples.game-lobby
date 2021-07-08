using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Relay.Http
{
    [Preserve]
    public class BasicError: IError
    {
        [Preserve]
        public string Type { get; }
        [Preserve]
        public string Title { get; }
        [Preserve]
        public int? Status { get; }
        [Preserve]
        public int Code { get; }
        [Preserve]
        public string Detail { get; }
        
        [Preserve]
        public BasicError(string type, string title, int? status, int code, string detail)
        {
            Type = type;
            Title = title;
            Status = status;
            Code = code;
            Detail = detail;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
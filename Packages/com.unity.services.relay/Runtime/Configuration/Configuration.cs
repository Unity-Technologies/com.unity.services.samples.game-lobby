using System.Collections.Generic;

namespace Unity.Services.Relay
{
    /// <summary>
    /// Represents a set of configuration settings
    /// </summary>
    public class Configuration
    {
        public string BasePath;
        public int? RequestTimeout;
        public int? NumberOfRetries;
        public IDictionary<string, string> Headers;
        

        public Configuration(string basePath, int? requestTimeout, int? numRetries, IDictionary<string, string> headers)
        {
            BasePath = basePath;
            RequestTimeout = requestTimeout;
            NumberOfRetries = numRetries;
            Headers = headers;
        }

        // Helper function for merging two configurations. Configuration `a` is
        // considered the base configuration if it is a valid object. Certain
        // values will be overridden if they are set to null within this
        // configuration by configuration `b` and the headers will be merged.
        public static Configuration MergeConfigurations(Configuration a, Configuration b)
        {
            // Check if either inputs are `null`, if they are, we return
            // whichever is not `null`, if both are `null`, we return `b` which
            // will be `null`. 
            if(a == null || b == null)
            {
                return a ?? b;
            }

            Configuration mergedConfig = a;

            if(mergedConfig.BasePath == null)
            {
                mergedConfig.BasePath = b.BasePath;
            }

            var headers = new Dictionary<string, string>();

            if (b.Headers != null)
            {
                foreach (var pair in b.Headers)
                {
                    headers[pair.Key] = pair.Value;
                }
            }

            if (mergedConfig.Headers != null)
            {
                foreach (var pair in mergedConfig.Headers)
                {
                    headers[pair.Key] = pair.Value;
                }
            }

            mergedConfig.Headers = headers;            
            mergedConfig.RequestTimeout = mergedConfig.RequestTimeout ?? b.RequestTimeout;
            mergedConfig.NumberOfRetries = mergedConfig.NumberOfRetries ?? b.NumberOfRetries;


            return mergedConfig;
        }
    }    
}

using System;
using System.Collections.Generic;
using System.Net;

namespace KidoZen
{
    public class ServiceEvent<T>
    {
        public HttpStatusCode StatusCode { get; internal set; }        
        public T Data { get; internal set; }
        public Dictionary<string, string> Headers { get; set; }

        public bool Succeed 
        { 
            get 
            { 
                return StatusCode >= HttpStatusCode.OK && StatusCode < HttpStatusCode.MultipleChoices;
            }
        }

        internal ServiceEvent<U> Clone<U>(U data = default(U))
        {
            return new ServiceEvent<U>
            {
                StatusCode = this.StatusCode,
                Data = data,
                Headers = this.Headers
            };
        }
    }
}

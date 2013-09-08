using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KidoZen
{
    public partial class Logging
    {
        KZApplication app;
        Uri endpoint;

        public Uri Url { get; private set; }

        internal Logging(KZApplication app, Uri endpoint)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (endpoint == null) throw new ArgumentNullException("endpoint");
            this.app = app;
            this.endpoint = endpoint;
            Url = endpoint;
        }

        public Task<ServiceEvent<JToken>> Clear()
        {
            return Url.ExecuteAsync<JToken>(app, method: "DELETE");
        }

        public Task<ServiceEvent<JToken>>  Write<T>(T message, LogLevel level)
        {
            return new Uri(Url, string.Format("?level={0}", (int)level)).ExecuteAsync<JToken>(app, message.ToJToken(), "POST");
        }

        public Task<ServiceEvent<IEnumerable<JToken>>> Query(string query = "{}", string options = null)
        {
            return Query<JToken>(query, options);
        }

        public Task<ServiceEvent<IEnumerable<T>>> Query<T>(string query = "{}", string options = null)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentNullException("query");

            var queryString = string.Format("?query={0}&options={1}",
                WebUtility.UrlEncode(query),
                WebUtility.UrlEncode(string.IsNullOrWhiteSpace(options) ? "{}" : options));

            return new Uri(Url, queryString).ExecuteAsync<IEnumerable<T>>(app);
        }

        public Task<ServiceEvent<IEnumerable<JToken>>> All()
        {
            return All<JToken>();
        }

        public Task<ServiceEvent<IEnumerable<T>>> All<T>()
        {
            return Query<T>("{}");
        }

    }
}


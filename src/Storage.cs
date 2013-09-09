using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KidoZen
{
    public partial class Storage
    {
        KZApplication app;
        Uri endpoint;

        public Uri Url { get; private set; }
        public String Name { get; private set; }
        public Indexes Indexes { get; private set; }

        internal Storage(KZApplication app, Uri endpoint)
            : this(app, endpoint, null)
        {
        }

        internal Storage(KZApplication app, Uri endpoint, string name)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (endpoint == null) throw new ArgumentNullException("endpoint");

            this.app = app;
            this.endpoint = endpoint;
            this.Url = endpoint.Concat(name);
            this.Name = name;
            this.Indexes = new Indexes(app, Url);
        }

        public Storage this[string name]
        {
            get 
            {
                if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

                return new Storage(app, endpoint, name);
            }
        }

        public Task<ServiceEvent<JToken>> Drop()
        {
            Validate();
            return Url.ExecuteAsync<JToken>(app, method:"DELETE");
        }

        public Task<ServiceEvent<JToken>> Delete(string id)
        {
            Validate();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            return Url.Concat(id).ExecuteAsync<JToken>(app, method: "DELETE");
        }

        public Task<ServiceEvent<JArray>> Query(string query = "{}", string options = "{}", string fields = "{}")
        {
            return DoQuery<JArray>(query, options, fields);
        }

        public Task<ServiceEvent<IEnumerable<T>>> Query<T>(string query = "{}", string options = "{}", string fields = "{}")
        {
            return DoQuery<IEnumerable<T>>(query, options, fields);
        }

        private Task<ServiceEvent<T>> DoQuery<T>(string query = "{}", string options = "{}", string fields = "{}")
        {
            Validate();
            var queryString = string.Format("?query={0}&options={1}&fields={2}",
                WebUtility.UrlEncode(query),
                WebUtility.UrlEncode(options),
                WebUtility.UrlEncode(fields));

            return new Uri(Url, queryString).ExecuteAsync<T>(app);
        }

        public Task<ServiceEvent<JToken>> Get(string id)
        {
            Validate();
            return Url.Concat(id).ExecuteAsync<JToken>(app);
        }

        public Task<ServiceEvent<T>> Get<T>(string id)
        {
            Validate();
            return Url.Concat(id).ExecuteAsync<T>(app);
        }

        public Task<ServiceEvent<StorageObject>> Save(StorageObject value, bool isPrivate = false)
        {
            return Save<StorageObject>(value, isPrivate);
        }

        public Task<ServiceEvent<StorageObject>> Save<T>(T value, bool isPrivate = false)
        {
            Validate();
            var obj = value.ToJToken() as JObject;
            if (obj == null) throw new ArgumentException("Value must be an object, it could not be a value type.", "value");

            var id = obj.Value<string>("_id");
            if (string.IsNullOrWhiteSpace(id))
                return Insert(obj, isPrivate);
            else
                return Update(id, obj);
        }

        public Task<ServiceEvent<StorageObject>> Insert(StorageObject value, bool isPrivate = false)
        {
            return Insert<StorageObject>(value, isPrivate);
        }

        public Task<ServiceEvent<StorageObject>> Insert<T>(T value, bool isPrivate = false)
        {
            Validate();
            return new Uri(Url, "?isPrivate=" + isPrivate.ToString()).ExecuteAsync<StorageObject>(app, value.ToJToken(), "POST");
        }

        public Task<ServiceEvent<StorageObject>> Update(StorageObject value)
        {
            return Update(value._id, value);
        }

        public Task<ServiceEvent<StorageObject>> Update<T>(string id, T value)
        {
            Validate();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
        
            return Url.Concat(id).ExecuteAsync<StorageObject>(app, value.ToJToken(), "PUT");
        }

        public Task<ServiceEvent<JArray>> All()
        {
            Validate();
            return Url.ExecuteAsync<JArray>(app);
        }

        public Task<ServiceEvent<IEnumerable<T>>> All<T>()
        {
            Validate();
            return Url.ExecuteAsync<IEnumerable<T>>(app);
        }

        private void Validate()
        {
            if (Name == null) throw new Exception(@"ObjectSet name is missing. Use Storage[""name""]");
        }
    }
}
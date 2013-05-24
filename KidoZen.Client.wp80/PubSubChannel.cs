using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using WebSocket4Net;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading;

namespace KidoZen
{
    public partial class PubSubChannel
    {
        public class PubSubException : Exception
        {
            public ushort Code { get; private set;}
            public PubSubException(ushort code, string message): base(message)
            {
                Code = code;
            }
        }

        private Uri endpoint;
        private Uri wsEndpoint;
        private WebSocket webSocket;
        private object sync = new object();
        
        public Uri Url { get; private set; }
        public string Name { get; private set; }
        KZApplication app;

        internal PubSubChannel(KZApplication app, Uri endpoint, Uri wsEndpoint):
            this(app, endpoint, wsEndpoint, null)
        {
        }

        internal PubSubChannel(KZApplication app, Uri endpoint, Uri wsEndpoint, string name)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (endpoint == null) throw new ArgumentNullException("endpoint");
            if (wsEndpoint == null) throw new ArgumentNullException("wsEndpoint");

            this.app = app;
            this.Name = name;
            this.Url = endpoint.Concat(name);
            this.endpoint = endpoint;
            this.wsEndpoint = wsEndpoint;
        }

        public PubSubChannel this[string name]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
                return new PubSubChannel(app, endpoint, wsEndpoint, name);
            }
        }

        public async Task<ServiceEvent<JToken>> Publish<T>(T message)
        {
            return await Url.ExecuteAsync<JToken>(app, message.ToJToken(), "POST");
        }

        public void Unsubscribe()
        {
            if (webSocket != null)
            {
                webSocket.Close();
                webSocket = null;
            }
        }

        public void Susbscribe<T>(Action<T> onMessage, Action<Exception> onError)
        {
            if (onMessage == null) throw new ArgumentNullException("onMessage");

            lock (sync)
            {
                if (webSocket == null) openSocket(onMessage, onError).Wait();
                webSocket.Send("bindToChannel::{\"application\":\"local\", \"channel\":\"" + Name + "\"}");
            }
        }

        private Task openSocket<T>(Action<T> onMessage, Action<Exception> onError)
        {
            return Task.Factory.StartNew(() =>
                {
                    var allDone = new ManualResetEvent(false);
                    var newWebSocket = new WebSocket(wsEndpoint.ToString());
                    newWebSocket.MessageReceived += (sender, args) =>
                    {
                        var colons = args.Message.IndexOf("::");
                        if (colons > -1)
                        {
                            var message = args.Message.Substring(colons + 2);
                            onMessage(JsonConvert.DeserializeObject<T>(message));
                        }
                    };

                    newWebSocket.Closed += (sender, args) =>
                    {
                        webSocket = null;
                    };

                    newWebSocket.Error += (sender, args) =>
                    {
                        if (onError != null) onError(args.Exception);
                        allDone.Set();
                    };

                    newWebSocket.Opened += (sender, args) =>
                    {
                        webSocket = newWebSocket; // Only store it after successfully connecting.
                        allDone.Set();
                    };

                    newWebSocket.Open();
                    allDone.WaitOne();
                });
        }
    }
}

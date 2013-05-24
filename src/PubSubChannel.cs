using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
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

        Uri endpoint;
        Uri wsEndpoint;
        KZApplication app;
        MessageWebSocket webSocket;
        
        public Uri Url { get; private set; }
        public string Name { get; private set; }

        internal PubSubChannel(KZApplication app, Uri endpoint, Uri wsEndpoint):
            this(app, endpoint, wsEndpoint, null)
        {
        }

        internal PubSubChannel(KZApplication app, Uri endpoint, Uri wsEndpoint, string name)
        {
            if (app == null) throw new ArgumentNullException("app");
            this.app = app;

            this.Url = endpoint.Concat(name);
            this.Name = name;
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
                webSocket.Dispose();
                webSocket = null;
            }
        }

        public async void Susbscribe<T>(Action<T> onMessage, Action<Exception> onError)
        {
            if (onMessage == null) throw new ArgumentNullException("onMessage");

            var newWebSocket = new MessageWebSocket();
            newWebSocket.Control.MessageType = SocketMessageType.Utf8;
            newWebSocket.MessageReceived += (sender, args) =>
            {
                using (var reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    var data = reader.ReadString(reader.UnconsumedBufferLength);
                    var colons = data.IndexOf("::") + 2;
                    var lengh = data.Length - colons;
                    var message = data.Substring(colons, lengh);
                    onMessage(JsonConvert.DeserializeObject<T>(message));
                }
            };

            newWebSocket.Closed += (sender, args) =>
            {
                var webSocketClosed = Interlocked.Exchange(ref webSocket, null);
                if (webSocketClosed != null)
                {
                    webSocketClosed.Dispose();
                    webSocket = null;
                }

                if (onError != null && args.Code != 1000) // 1000 = OK
                {
                    onError(new PubSubException(args.Code, args.Reason)); 
                }
            };

            await newWebSocket.ConnectAsync(wsEndpoint);
            webSocket = newWebSocket; // Only store it after successfully connecting.
            using (var writer = new DataWriter(webSocket.OutputStream))
            {
                writer.WriteString("bindToChannel::{\"application\":\"local\", \"channel\":\"" + Name + "\"}");
                await writer.StoreAsync();
            }
        }
    }
}

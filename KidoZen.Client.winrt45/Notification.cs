using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Networking.PushNotifications;
using System.Threading;

namespace KidoZen
{
    public partial class Notification
    {
        #region inner type

        public class CreateSubscriptionBody
        {
            public CreateSubscriptionBody(Uri id)
            {
                platform = "wns";
                subscriptionId = id.ToString();
                deviceId = KZApplication.GetDeviceUniqueID();
            }

            public string platform;
            public string subscriptionId;
            public string deviceId;
        }

        #endregion

        private object sync = new Object();
        private const string CHANNEL_PREFIX = "kidozen.";
        private KZApplication app;
        private PushNotificationChannel channel = null;

        internal string deviceId = KZApplication.GetDeviceUniqueID();
        
        public string ChannelName { get; private set; }
        public Uri Url { get; private set; }

        internal Notification(KZApplication app, Uri endpoint)
        {
            if (app == null) throw new ArgumentNullException("app");
            this.app = app;

            this.Url = endpoint;
            this.ChannelName = CHANNEL_PREFIX + app.Name;

            app.OnAuthentication += OnUserAuthenticated;
        }

        #region private members

        private async void OnUserAuthenticated(object sender, EventArgs e)
        {
            // If the device has a subscription
            // initialize Windows Phone infrastrucutre
            try
            {
                var se = await GetSubscriptionsCount();
                if (se.Data > 0)
                {
                    var reult = EnableNotifications();
                }
            }
            catch { }
        }

        private async Task<PushNotificationChannel> EnableNotifications()
        {
            Monitor.Enter(sync); //TODO:
            try
            {
                channel = await PushNotificationChannelManager
                    .CreatePushNotificationChannelForApplicationAsync()
                    .AsTask();
                return channel;
            }
            finally
            {
                Monitor.Exit(sync);
            }
        }

        private void DisableNotifications()
        {
            lock (sync)
            {
                if (channel == null) return;

                channel.Close();
                channel = null;
            }
        }

        private async Task<ServiceEvent<JToken>> doSubscribe(string channelName)
        {
            if (channel == null)
            {
                await EnableNotifications();
            }

            var resource = "/subscriptions/"
                + WebUtility.UrlEncode(app.Name)
                + (string.IsNullOrWhiteSpace(channelName) ? "" : "/" + WebUtility.UrlEncode(channelName));
            var body = new CreateSubscriptionBody(new Uri(channel.Uri, UriKind.Absolute));
            return await Url.Concat(resource).ExecuteAsync<JToken>(app, body.ToJToken(), "POST");
        }

        private async Task<ServiceEvent<JToken>> doUnsubscribe(string channelName)
        {
            var resource = "/subscriptions/"
                + WebUtility.UrlEncode(app.Name) + "/"
                + WebUtility.UrlEncode(channelName) + "/"
                + WebUtility.UrlEncode(channel.Uri);

            return await Url.Concat(resource).ExecuteAsync<JToken>(app, method:"DELETE")
                .ContinueWith<ServiceEvent<JToken>>(se =>
                {
                    var result = GetSubscriptionsCount().Result;
                    if (result.Data == 0) DisableNotifications();
                    return se.Result;
                });
        }

        #endregion

        public async Task<PushNotificationChannel> GetChannel()
        {
            if (channel!=null) return channel;
            return await EnableNotifications();
        }

        public async Task<ServiceEvent<string[]>> GetSubscriptions()
        {
            var resource = "/devices/" + WebUtility.UrlEncode(deviceId) + "/" + WebUtility.UrlEncode(app.Name);

            var task = await Url.Concat(resource).ExecuteAsync<JArray>(app);

            string[] result = null;
            if ((int)(task.StatusCode) < 300)
            {
                result = task.Data
                    .Select(subscription => subscription.Value<string>("channelName"))
                    .ToArray();
            }

            return task.Clone<string[]>(result);
        }

        public async Task<ServiceEvent<int>> GetSubscriptionsCount()
        {
            var resource = "/devices/" + WebUtility.UrlEncode(deviceId) + "/" + WebUtility.UrlEncode(app.Name) + "?count=true";
            return await Url.Concat(resource).ExecuteAsync<int>(app);
        }

        public async Task<ServiceEvent<JToken>> Subscribe(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName)) throw new ArgumentNullException("channelName");
            return await doSubscribe(channelName);
        }

        public async Task<ServiceEvent<JToken>> Unsubscribe(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName)) throw new ArgumentNullException("channelName");
            return await doUnsubscribe(channelName);
        }

        public async Task<ServiceEvent<JToken>> Push(string channelName, NotificationData data)
        {
            var resource = "/push/"
                + WebUtility.UrlEncode(app.Name) + "/"  
                + WebUtility.UrlEncode(channelName);

            return await Url.Concat(resource).ExecuteAsync<JToken>(app, data.ToJToken(), "POST");
        }
    }
}
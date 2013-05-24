using System;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Phone.Info;
using Microsoft.Phone.Notification;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KidoZen
{
    public partial class Notification
    {
        #region inner type

        public class CreateSubscriptionBody
        {
            public CreateSubscriptionBody(Uri id)
            {
                platform = "mpns";
                subscriptionId = id.ToString();
                deviceId = KZApplication.GetDeviceUniqueID();
            }

            public string platform;
            public string subscriptionId;
            public string deviceId;
        }

        #endregion

        const string CHANNEL_PREFIX = "kidozen.";
        object sync = new Object();
        string deviceId = KZApplication.GetDeviceUniqueID();
        KZApplication app;
        HttpNotificationChannel channel = null;

        public string ChannelName { get; private set; }
        public Uri Url { get; private set; }

        internal Notification(KZApplication app, Uri endpoint)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (endpoint == null) throw new ArgumentNullException("endpoint");

            this.app = app;
            this.Url = endpoint;
            this.ChannelName = CHANNEL_PREFIX + app.Name;

            app.OnAuthentication += async (sender, e) =>
            {
                // If the device has a subscription
                // initialize Windows Phone infrastrucutre
                var resultCount = await GetSubscriptionsCount();
                if (resultCount.Data > 0) EnableNotifications();
            };
        }

        #region private members

        private void EnableNotifications()
        {
            if (channel != null) return;

            channel = HttpNotificationChannel.Find(ChannelName);
            if (channel == null)
            {
                channel = new HttpNotificationChannel(ChannelName);
                WireChannelEvents(channel);
                channel.Open();
                BindToShell(channel);
            }
            else
            {
                WireChannelEvents(channel);
                BindToShell(channel);
                var noCompileWarn = doSubscribe(null);
            }
        }

        private void DisableNotifications()
        {
            if (channel == null) return;

            channel.Close();
            channel.Dispose();
            channel = null;
        }

        private void WireChannelEvents(HttpNotificationChannel channel)
        {
            channel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(onChannelUriUpdated);
        }

        private void BindToShell(HttpNotificationChannel channel)
        {
            if (channel == null) return;
            if (!channel.IsShellToastBound)
            {
                channel.BindToShellToast();
            }

            if (!channel.IsShellTileBound)
            {
                channel.BindToShellTile();
            }
        }

        void onChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            var aux = doSubscribe(null);
        }

        private async Task<ServiceEvent<JToken>> doSubscribe(string channelName)
        {

            if (channel == null)
            {
                EnableNotifications();
            }

            if (channel.ChannelUri == null)
            {
                return new ServiceEvent<JToken>();
            }

            var resource = "/subscriptions/"
                + HttpUtility.UrlEncode(app.Name)
                + (string.IsNullOrWhiteSpace(channelName) ? "" : "/" + HttpUtility.UrlEncode(channelName));

            return await new Uri(resource).ExecuteAsync<JToken>(app, new CreateSubscriptionBody(channel.ChannelUri).ToJToken(), "POST");
        }

        private async Task<ServiceEvent<JToken>> doUnsubscribe(string channelName)
        {
            var resource = "/subscriptions/"
                + HttpUtility.UrlEncode(app.Name) + "/"
                + HttpUtility.UrlEncode(channelName) + "/"
                + HttpUtility.UrlEncode(channel.ChannelUri.ToString());

            var result = await new Uri(resource).ExecuteAsync<JToken>(app, method: "DELETE");
            var countResult = await GetSubscriptionsCount();
            if (countResult.Data == 0) DisableNotifications();
            return result;
        }

        #endregion


        public HttpNotificationChannel Channel
        {
            get
            {
                if (channel == null) EnableNotifications();
                return channel;
            }
        }

        public async Task<ServiceEvent<string[]>> GetSubscriptions()
        {
            var resource = "/devices/" + HttpUtility.UrlEncode(deviceId) + "/" + HttpUtility.UrlEncode(app.Name);
            var result = await new Uri(resource).ExecuteAsync<JArray>(app);

            string[] subscriptions = null;
            if (result.Succeed)
            {
                subscriptions = result.Data
                    .Select(subscription => subscription.Value<string>("channelName"))
                    .ToArray();
            }
            return result.Clone<string[]>(subscriptions);
        }

        public async Task<ServiceEvent<int>> GetSubscriptionsCount()
        {
            var resource = "/devices/" + HttpUtility.UrlEncode(deviceId) + "/" + HttpUtility.UrlEncode(app.Name) + "?count=true";
            return await Url.Concat(resource).ExecuteAsync<int>(app);
        }

        public async Task<ServiceEvent<JToken>> Subscribe(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName)) throw new ArgumentNullException("channelName");
            return await doSubscribe(channelName);
        }

        public async Task<ServiceEvent<JToken>>Unsubscribe(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName)) throw new ArgumentNullException("channelName");
            return await doUnsubscribe(channelName);
        }

        public async Task<ServiceEvent<JToken>> Push(string channelName, NotificationData data)
        {
            var resource = "/push/"
                + HttpUtility.UrlEncode(app.Name) + "/"
                + HttpUtility.UrlEncode(channelName);

            return await new Uri(resource).ExecuteAsync<JToken>(app, data.ToJToken(), "POST");
        }
    }
}
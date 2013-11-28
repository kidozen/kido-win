﻿using System;
using System.Net;
using System.Threading.Tasks;
namespace KidoZen
{
    public class SmsSender
    {
        KZApplication app;
        Uri endpoint;

        public Uri Url { get; private set; }
        public String Number { get; private set; }
        
        internal SmsSender(KZApplication app, Uri endpoint)
            : this(app, endpoint, null)
        {
        }

        internal SmsSender(KZApplication app, Uri endpoint, string number)
        {
            if (app == null) throw new ArgumentNullException("app");
            this.app = app;

            this.endpoint = endpoint;
            Url = endpoint;
            Number = number;
        }

        public SmsSender this[string number]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(number)) throw new ArgumentNullException("number");
                return new SmsSender(app, endpoint, number);
            }
        }

        public Task<ServiceEvent<SMSStatus>> Send(string message)
        {
            Validate();
            return Url
                .Concat("?to=" + WebUtility.UrlEncode(Number) + "&message=" + WebUtility.UrlEncode(message))
                .ExecuteAsync<SMSStatus>(app, method:"POST");
        }

        public Task<ServiceEvent<SMSStatus>> GetStatus(string messageId)
        {
            Validate();
            if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentNullException("messageId");

            return Url
                .Concat(string.Format("/{0}", messageId))
                .ExecuteAsync<SMSStatus>(app);
        }

        private void Validate()
        {
            if (Number == null) throw new Exception(@"SMSSender number is missing. Use SmsSender[""+155555555""]");
        }
    }
}
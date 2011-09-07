using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Hammock.Authentication.OAuth;
using Newtonsoft.Json.Linq;
using Hammock;
using Hammock.Web;

namespace TwitterRealTime
{
    public class TwitterStream
    {
        StreamReader _stream;
        OAuthCredentials _credential;
        public RestRequest Request { get; private set; }

        public bool IsStreaming { get; private set; }

        private string _authority;

        public event NewJSONHandler OnNewJsonObject;
        public delegate void NewJSONHandler(JObject obj);

        internal TwitterStream(OAuthCredentials credential, string authority, RestRequest request=null)
        {
            _credential = credential;
            _authority = authority;
            if (request != null)
                Request = request;
            else
                Request = new RestRequest();
        }

        /// <summary>
        ///  Starts streaming
        /// </summary>
        public System.Net.HttpStatusCode Start()
        {
            if (IsStreaming)
                return System.Net.HttpStatusCode.OK;

            var client = new RestClient()
            {
                Authority = _authority,
                Credentials = _credential
            };           

            var response = client.Request(Request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                _stream = new StreamReader(response.ContentStream, encode);
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(stream));
            }
            return response.StatusCode;
        }

        private void stream(object o)
        {
            IsStreaming = true;

            while (IsStreaming)
            {
                try
                {                    
                    string text = _stream.ReadLine();
                    System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(process), text);                    
                }
                catch (Exception) { }
            }
        }

        private void process(object text)
        {
            if (OnNewJsonObject != null)
            {
                try
                {
                    OnNewJsonObject(JObject.Parse(text as string));
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Stops streaming
        /// </summary>
        public void Stop()
        {
            IsStreaming = false;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using Hammock.Authentication.OAuth;
using Hammock;
using Hammock.Web;
using System.Diagnostics;
using System.Web;

namespace TwitterRealTime
{
    public class TwitterClient
    {
        
        public bool Authenticated { get {return _credentials!=null;} }

        private OAuthCredentials _credentials;

        public OAuthCredentials Credentials
        {
            get 
            {
                if (Authenticated)
                    return _credentials;
                else
                {
                    Authenticate();
                    return _credentials;
                }

            }
            private set
            {
                _credentials = value;
            }
        }

        public string AccessToken
        {
            get;
            private set;            
        }

        public string AccessTokenSecret
        {
            get;
            private set;
        }

        public string ConsumerKey
        {
            get;
            private set;
        }

        public string ConsumerSecret
        {
            get;
            private set;
        }

        public delegate void AuthenticationHandler(ref string pin);
        public event AuthenticationHandler OnRequestAuthenticationResponse;        

        /// <summary>
        /// Creates a streaming client and authenticates the client for reading from the logged in user
        /// </summary>
        /// <param name="consumerkey"></param>
        /// <param name="consumersecret"></param>
        public TwitterClient(string consumerkey, string consumersecret, string accessToken, string accessTokenSecret)
        {
            ConsumerKey = consumerkey;
            ConsumerSecret = consumersecret;

            AccessToken = accessToken;
            AccessTokenSecret = accessTokenSecret;

            if (AccessToken != "" && AccessTokenSecret != "")
                _credentials = new OAuthCredentials
                {
                    Type = OAuthType.ProtectedResource,
                    Token = AccessToken,
                    TokenSecret = AccessTokenSecret,
                    ConsumerKey = ConsumerKey,
                    ConsumerSecret = ConsumerSecret
                };
        }        

        /// <summary>
        /// Authenticates the application using OAuth with PIN code verification
        /// </summary>
        public void Authenticate()
        {
            if (Authenticated)
                return;
            // POST consumer_key and consumer_secret to obtain request token
            OAuthCredentials credential = new OAuthCredentials()
            {
                Type = OAuthType.RequestToken,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret
            };

            RestClient client = new RestClient()
            {
                Authority = "https://api.twitter.com/oauth",
                Credentials = credential
            };

            var request = new RestRequest
            {
                Path = "/request_token",
                Method = WebMethod.Post
            };
            RestResponse response = client.Request(request);
            var collection = System.Web.HttpUtility.ParseQueryString(response.Content);
            string requestToken = collection["oauth_token"];
            string requestTokenSecret = collection["oauth_token_secret"];


            // Obtain the PIN code
            Process.Start("http://api.twitter.com/oauth/authorize?oauth_token=" + requestToken);
            string code = null;
            OnRequestAuthenticationResponse(ref code);

            // Exchange Pin code for Access token

            // POST consumer_key and consumer_secret to obtain request token
            credential = new OAuthCredentials()
            {
                Type = OAuthType.AccessToken,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret,
                Token = requestToken,
                TokenSecret = requestTokenSecret,
                Verifier = code
            };

            client = new RestClient()
            {
                Authority = "https://api.twitter.com/oauth",
                Credentials = credential
            };

            request = new RestRequest
            {
                Path = "/access_token",
                Method = WebMethod.Post
            };
            response = client.Request(request);
            collection = HttpUtility.ParseQueryString(response.Content);
            AccessToken = collection["oauth_token"];
            AccessTokenSecret = collection["oauth_token_secret"];

            this._credentials = new OAuthCredentials
            {
                Type = OAuthType.ProtectedResource,
                Token = AccessToken,
                TokenSecret = AccessTokenSecret,
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret
            };
        }

        /// <summary>
        /// Returns the current top 10 trending topics on Twitter. The response includes the time of the request, 
        /// the name of each trending topic, and query used on Twitter Search results page for that topic.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Trends
        {
            get
            {
                RestClient client = new RestClient()
                {
                    Authority = "https://api.twitter.com/1/trends",
                    Credentials = _credentials
                };

                var request = new RestRequest
                {
                    Path = "/current.json",
                    Method = WebMethod.Get
                };
                var response = client.Request(request);
                var array = JObject.Parse(response.Content)["trends"];
                foreach (JToken o in array.Children().Children().Children())
                    yield return o["name"].Value<string>();
            }
        }


        /// <summary>
        /// Creates a TwitterStream that retrieves real-time result from the REST verb statuses/filter
        /// </summary>
        /// <param name="keywords"></param>
        /// <returns></returns>
        public TwitterStream CreateStatusFilter(params string[] keywords)
        {
            TwitterStream stream = new TwitterStream(_credentials, "http://stream.twitter.com/1/statuses");
            string query = "";
            foreach (string key in keywords)
            {                
                query += key;
                if (key != keywords[keywords.Length - 1])
                    query += ",";
            }
            stream.Request.AddParameter("track", query);
            stream.Request.Path = "filter.json";
            stream.Request.Method = WebMethod.Post;
            return stream;
        }

        /// <summary>
        /// Creates a Twitter from the REST verb statuses/sample
        /// </summary>
        /// <returns>A TwitterStream object that can be used for sampling</returns>
        public TwitterStream CreateGardenHose()
        {            
            TwitterStream stream = new TwitterStream(_credentials, "http://stream.twitter.com/1/statuses");
            stream.Request.Method = WebMethod.Get;
            stream.Request.Path = "/sample.json?delimited=true";
            return stream;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NETGen.Core;
using NETGen.Visualization;

using System.Runtime.CompilerServices;

namespace TwitterRealTime
{

    public class TwitterRealTime
    {
        static string ConsumerKey = "t9isxC1F0NLObQ9IX8xng";
        static string ConsumerSecret = "jDlzHVlF0w5AgR6xqN75CBHYJWpSHl3FkrVqgVgPc8M";
        static string AccessToken = Properties.Settings.Default.AccessToken;
        static string AccessTokenSecret = Properties.Settings.Default.AccessTokenSecret;

        static TwitterClient client;
        static TwitterStream stream;

        static string queryString;
        static Network network;

        static int counter = 0;

        public static void Main(string[] args)
        {
            client = new TwitterClient(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
               

            client.OnRequestAuthenticationResponse += new TwitterClient.AuthenticationHandler(client_OnRequestAuthenticationResponse);

            if (!client.Authenticated)
                client.Authenticate();

            Properties.Settings.Default.AccessToken = client.AccessToken;
            Properties.Settings.Default.AccessTokenSecret = client.AccessTokenSecret;
            Properties.Settings.Default.Save();

            queryString = client.Trends.ToArray()[0];               

            Console.WriteLine("Starting monitoring of trend: "+queryString);
            network = new Network();

            //NetworkVisualizer visualizer = new NetworkVisualizer(network, new NETGen.Layout.FruchtermanReingold.FruchtermanReingoldLayout(10), new PresentationSettings(2000d, 1000d, 0d));

            //NetworkDisplay display = new NetworkDisplay(visualizer);

            counter = 0;
            stream = client.CreateStatusFilter(client.Trends.ToArray()[0]);
            stream.OnNewJsonObject += new TwitterStream.NewJSONHandler(stream_OnNewJsonObject);
            stream.Start();

            Console.ReadKey();

            stream.Stop();
        }

        static void client_OnRequestAuthenticationResponse(ref string pin)
        {
            Console.WriteLine("Please enter PIN: ");
            pin = Console.ReadLine();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        static void stream_OnNewJsonObject(Newtonsoft.Json.Linq.JObject obj)
        {
            counter++;
            var user = obj["user"];
            if (user != null)
            {
                string screenname = (string)user["screen_name"];

                foreach (var mention in obj["entities"]["user_mentions"])
                {
                    string mention_screenname = (string)mention["screen_name"];
                    if (screenname != null && mention_screenname != null)
                    {
                        Vertex v1 = null, v2 = null;
                        if (!network.ContainsVertex(screenname))
                            v1 = network.CreateVertex(screenname);
                        else
                            v1 = network.SearchVertex(screenname);
                        if (!network.ContainsVertex(mention_screenname))
                            v2 = network.CreateVertex(mention_screenname);
                        else
                            v2 = network.SearchVertex(mention_screenname);
                        if (!v1.IsPredecessor(v2))
                            network.CreateEdge(v1, v2, EdgeType.Undirected);
                    }
                }
            }
            Console.WriteLine("Collected {0} tweets ...", counter);
        }
    }
}

using System;
using System.Web;
using System.Collections.Generic;

using Hammock;
using Hammock.Web;
using Hammock.Authentication;
using Newtonsoft.Json.Linq;

namespace gitHubParser
{
	public class gitHubClient
	{
		private Hammock.Authentication.Basic.BasicAuthCredentials credentials;
		
		private string authority = "https://api.github.com";
		
		public gitHubClient(string user, string password)
		{
			this.credentials = new Hammock.Authentication.Basic.BasicAuthCredentials();
			this.credentials.Password = password;
			this.credentials.Username = user;
		}
		
		public string[] GetCollaborators(string user, string repos)
		{
			RestClient client = new RestClient()
                {
                    Authority = authority,
                    Credentials = credentials
                };			

                var request = new RestRequest
                {
                    Path = string.Format("/repos/{0}/{1}/collaborators", user, repos),
                    Method = WebMethod.Get
                };
                var response = client.Request(request);
                List<string> collabs = new List<string>();
			
                var array = JArray.Parse(response.Content);
				foreach(JObject o in array)
					collabs.Add(o["login"].ToString());		
				
				return collabs.ToArray();
		}
		
		public string[] GetRepositories(string user)
		{
			RestClient client = new RestClient()
                {
                    Authority = authority,
                    Credentials = credentials
                };

                var request = new RestRequest
                {
                    Path = string.Format("/users/{0}/repos",user),
                    Method = WebMethod.Get
                };
                var response = client.Request(request);
				List<string> repos = new List<string>();
			
                var array = JArray.Parse(response.Content);
				foreach(JObject o in array)
					repos.Add(o["name"].ToString());		
				
				return repos.ToArray();
		}
		
		public string[] GetCommittedFiles(string user, string repos, string committer)
		{
			RestClient client = new RestClient()
                {
                    Authority = authority,
                    Credentials = credentials
                };

                var request = new RestRequest
                {
                    Path = string.Format("/repos/{0}/{1}/commits",user, repos),
                    Method = WebMethod.Get
                };
                var response = client.Request(request);
				List<string> commitSHAs = new List<string>();
			
                var array = JArray.Parse(response.Content);
				foreach(JObject o in array)
					if (o["committer"]["name"].ToString()==committer)
						commitSHAs.Add(o["sha"].ToString());
			
				List<string> files = new List<string>();
			
				foreach(string sha in commitSHAs)
				{
					client = new RestClient()
	                {
	                    Authority = authority,
	                    Credentials = credentials
	                };
	
	                request = new RestRequest
	                {
	                    Path = string.Format("/repos/{0}/{1}/commits/{2}",user, repos, sha),
	                    Method = WebMethod.Get
	                };
	                response = client.Request(request);
				
					var commit_files = JObject.Parse(response.Content)["files"];
					foreach(string f in commit_files)
						files.Add(f);					
				}				
				return files.ToArray();
		}
		
		
	}
}


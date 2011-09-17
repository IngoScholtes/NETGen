using System;

namespace gitHubParser
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			gitHubClient client = new gitHubClient("XXX", "YYY");			string[] repos = client.GetRepositories("IngoScholtes");
			
			Console.WriteLine("Repositories of User IngoScholtes...");
			foreach(string s in repos)
				Console.WriteLine("\t" + s);
			
			string[] collaborators = client.GetCollaborators("mono", "mono");
			Console.WriteLine("Collaborators of Repository mono...");
			foreach(string s in collaborators)
				Console.WriteLine("\t" + s);
			
			string[] commitedFiles = client.GetCommittedFiles("IngoScholtes", "NETGen", "IngoScholtes");
			Console.WriteLine("Files commited by migueldeicaza in mono...");
			foreach(string s in commitedFiles)
				Console.WriteLine("\t" + s);
		}
	}
}

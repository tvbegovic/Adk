using System.Net;

namespace AdK.Tagger.Model
{
	public static class WebUtility
	{
		public static bool Exists(string url)
		{
			bool exists = false;

			var webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Method = "HEAD";

			try
			{
				using (var response = webRequest.GetResponse())
					exists = true;
			}
			catch (WebException)
			{
				// A WebException will be thrown if the status of the response is not '200 OK'
			}
			return exists;
		}
		public static string DownloadString(string url)
		{
			string content = null;
			using (var webClient = new WebClient())
			{
				try { content = webClient.DownloadString(url); }
				catch (WebException wex)
				{
					if (wex.Response is HttpWebResponse && (wex.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
						content = null; // missing file
				}
			}
			return content;
		}
	}
}
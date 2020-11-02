using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public static class Notificator
	{
		private static object _lock = new object();

		public static List<TaggerUser> SendTranscriptAvailable()
		{
			if (_ShouldSend())
			{
				var transcribers = _GetInactiveTranscribers();

				var config = Transcript.Configuration.Get();
				string transcriptModuleUrl = string.Format("<a href=\"{0}\">{0}</a>", Settings.Get("Tagger", "RootUrl") + "?page=transcript");
				foreach (var transcriber in transcribers)
					Mailer.Send(
						transcriber.Email,
						config.OrderNotificationSubject
							.Replace("[transcriberName]", transcriber.DisplayName)
							.Replace("[transcriptUrl]", transcriptModuleUrl),
						config.OrderNotificationBody
							.Replace("[transcriberName]", transcriber.DisplayName)
							.Replace("[transcriptUrl]", transcriptModuleUrl),
						isHtml: true);

				return transcribers;
			}
			return new List<TaggerUser>();
		}
		public static List<TaggerUser> RemindAwaitingTranscripts(string subject, string body)
		{
			var transcribersAndQueue = GetTranscribersHavingQueue();

			string transcriptModuleUrl = string.Format("<a href=\"{0}\">{0}</a>", Settings.Get("Tagger", "RootUrl") + "?page=transcript");
			foreach (var transcriberAndQueue in transcribersAndQueue)
			{
				if (transcriberAndQueue.QueueLength > 0)
					Mailer.Send(
						transcriberAndQueue.Transcriber.Email,
						subject
							.Replace("[transcriberName]", transcriberAndQueue.Transcriber.DisplayName)
							.Replace("[transcriptUrl]", transcriptModuleUrl)
							.Replace("[transcriptCount]",transcriberAndQueue.QueueLength.ToString()),
						body
							.Replace("[transcriberName]", transcriberAndQueue.Transcriber.DisplayName)
							.Replace("[transcriptUrl]", transcriptModuleUrl)
							.Replace("[transcriptCount]", transcriberAndQueue.QueueLength.ToString()),
						isHtml: true);
			}

			return transcribersAndQueue
				.Select(a => a.Transcriber)
				.ToList();
		}
		public static List<TranscriberAndQueue> GetTranscribersHavingQueue()
		{
			// Only keep transcribers that haven't an empty queue
			return _GetInactiveTranscribers()
				.Select(transcriber => new TranscriberAndQueue
				{
					Transcriber = transcriber,
					QueueLength = Model.Transcript.GetQueueLength(transcriber.Id)
				})
				.Where(a => a.QueueLength > 0)
				.ToList();
		}
		public class TranscriberAndQueue
		{
			public TaggerUser Transcriber;
			public int QueueLength;
		}

		private static TimeSpan _NotificationThrottleDelay()
		{
			int delay = Transcript.Configuration.Get().NotificationThrottleDelay;
			return TimeSpan.FromMinutes(delay);
		}
		private static bool _ShouldSend()
		{
			lock (_lock)
			{
				var _LastSent = Transcript.Configuration.GetLastSent();
				bool shouldSend = !_LastSent.HasValue || _LastSent < DateTime.UtcNow.Add(-_NotificationThrottleDelay());
				if (shouldSend)
					Transcript.Configuration.SetLastSend(DateTime.UtcNow);
				return shouldSend;
			}
		}
		private static List<TaggerUser> _GetInactiveTranscribers()
		{
			var transcribers = TaggerUser.GetAll()
				.Where(user => user.IsGrantedTranscript())
				.ToList();

			var activeUserIds = TranscriptConcurrency.GetActiveUserIds();
			var inactiveTranscribers = transcribers.Where(user => !activeUserIds.Contains(user.Id)).ToList();
			return inactiveTranscribers;
		}
	}
}
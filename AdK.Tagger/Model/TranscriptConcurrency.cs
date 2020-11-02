using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class TranscriptConcurrency
	{
		private static object _lock = new object();

		private static HashSet<SampleBeingTranscribed> _BeingTranscribed = new HashSet<SampleBeingTranscribed>();

		public static bool StartTranscribing(string userId, Guid songId, bool training, bool review)
		{
			lock (_lock)
			{
				if (!_BeingTranscribed.Any(match => match.UserId == userId && match.SongId == songId))
				{
					_BeingTranscribed.Add(new SampleBeingTranscribed
					{
						UserId = userId,
						SongId = songId,
						Training = training,
						Review = review,
						Expiration = DateTime.UtcNow.AddMinutes(30)
					});
					return true;
				}
				return false;
			}
		}
		public static SampleBeingTranscribed EndTranscribing(string userId, Guid songId)
		{
			lock (_lock)
			{
				_BeingTranscribed.RemoveWhere(match => DateTime.UtcNow > match.Expiration);
				var sbt = _BeingTranscribed.FirstOrDefault(match => match.UserId == userId && match.SongId == songId);
				_BeingTranscribed.RemoveWhere(match => match.UserId == userId && match.SongId == songId);
				return sbt;
			}
		}
		public static List<Guid> GetSongIds()
		{
			lock (_lock)
			{
				return _BeingTranscribed.Select(match => match.SongId).ToList();
			}
		}
		public static List<string> GetActiveUserIds()
		{
			lock (_lock)
			{
				return _BeingTranscribed.Select(match => match.UserId).Distinct().ToList();
			}
		}

		public static void EndAll()
		{
			_BeingTranscribed.Clear();
		}
	}

	public class SampleBeingTranscribed
	{
		public string UserId;
		public Guid SongId;
		public bool Training;
		public bool Review;
		public DateTime Expiration;
	}
}

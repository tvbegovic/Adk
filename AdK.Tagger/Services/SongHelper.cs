using AdK.Tagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Services
{
    public class SongHelper
    {
        /// <summary>
        /// Fix issue with songs that have duration 0
        /// </summary>
        /// <param name="song"></param>
        public static List<Song> Sanitize(List<Song> songs, TaggerUser user)
        {
            try {
                var unProcessedSongs = songs.Where(s => s.Status == (int)SongStatus.New && !String.IsNullOrWhiteSpace(s.PksId));

                if (unProcessedSongs.Any()) {
                    var pk = new PkConnector(user);
                    foreach (var song in unProcessedSongs) {
                        var pkStatus = pk.GetSampleStatus(song.PksId);

                        if (pkStatus.Status == PkSampeStatusType.OK) {
                            song.Duration = pkStatus.Duration;
                            song.Status = (int)SongStatus.Processed;
                            Song.UpdateDurationAndStatus(song.PksId, pkStatus.Duration, SongStatus.Processed);
                        }

                        else if (pkStatus.Status != PkSampeStatusType.QUEUED) {
                            Song.UpdateStatus(song.Id, SongStatus.Processed);
                        }

                    };
                }
            } catch(Exception err) {
                App.Log.Error(err);
            }

            return songs;
        }

    }
}
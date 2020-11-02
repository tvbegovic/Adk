using System.IO;
using AdK.Tagger.Model;
using AdK.Tagger.Model.AppSettings;
using AdK.Tagger.Model.Audit;
using AdK.Tagger.Model.MailTemplates;
using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data;
using mhr = AdK.Tagger.Model.MediaHouseReport;
using AdK.Tagger.Services;
using System.Web.Caching;
using System.Globalization;
using System.Configuration;

namespace AdK.Tagger
{
    [WebService(Namespace = "http://playkontrol.eu/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class Service : WebService
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();


        static Service()
        {

            if (Model.Application.IsAdK)
            {

				Log.Info("Running batch process for adk");
				
                /*var transcriptQueueingService = new Model.TranscriptQueueingService();
                transcriptQueueingService.Start();*/
#if !DEBUG
                var matchesImportService = new Model.MatchesImportService();
                matchesImportService.Start();

				var feedNewMatchCountService = new FeedNewMatchCountService();
                feedNewMatchCountService.Start();
#endif

			}

			if (Model.Application.IsDokaznice)
            {
                Log.Info("Running batch process for dokaznice");

                var songScanBatchMailerService = new SongScanBatchMailerService();
                songScanBatchMailerService.Start();
            }

        }


        #region Login and account
        [WebMethod]
        public AjaxUser TestToken()
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                    return AjaxUser.FromTaggerUser(user);

                _Token = null; // Clear the cookie if it's invalid
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public AjaxUser Authenticate(string email, string password)
        {
            try
            {
                var userAndToken = Model.TaggerUser.Authenticate(email, password, _DeviceId);
                if (userAndToken != null)
                {
                    _Token = userAndToken.Item2;
                    return AjaxUser.FromTaggerUser(userAndToken.Item1);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public bool ChangePassword(string formerPassword, string newPassword)
        {
            try
            {
                bool changed = false;
                var user = _GetUser();
                if (user != null && user.VerifyPassword(formerPassword))
                {
                    user.UpdatePassword(newPassword);
                    changed = true;
                }
                return changed;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public bool LostPassword(string email)
        {
            try
            {
                bool done = false;
                var user = Model.TaggerUser.LoadByEmail(email);
                if (user != null)
                {
                    string recoveryToken = user.ResetPasswordRecoveryToken();
                    _SendPasswordRecoveryLink(user, recoveryToken);
                    done = true;
                }
                return done;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        private void _SendPasswordRecoveryLink(Model.TaggerUser user, string emailToken)
        {
            Uri url = HttpContext.Current.Request.Url;
            string validationLink = string.Format("{0}://{1}{2}/login.html?passwordRecovery={3}",
                 url.Scheme,
                 url.Authority,
                 HttpRuntime.AppDomainAppVirtualPath == "/" ? "" : HttpRuntime.AppDomainAppVirtualPath,
                 emailToken);

            Model.Mailer.Send(user.Email, "PlayKontrol password recovery", "Please click the link below to change your password:\n\n" + validationLink);
        }
        [WebMethod]
        public AjaxUser ChangeRecoveredPassword(string password, string passwordToken)
        {
            try
            {
				string email;
                if (Model.TaggerUser.ChangePassword(password, passwordToken, out email))
                    return Authenticate(email, password);
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public bool SaveAccount(Model.Account account)
        {
            try
            {
                bool saved = false;
                var user = _GetUser();
                if (user != null)
                {
                    user.UpdateAccount(account);
                    saved = true;
                }
                return saved;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public bool CreateAccount(AjaxUser user)
        {
            try
            {
                var dbUser = Model.TaggerUser.LoadByEmail(user.email);
                if (dbUser == null)
                {
                    string emailToken = Model.TaggerUser.Create(user.email, user.password);
                    dbUser = Model.TaggerUser.LoadByEmail(user.email);
                    Model.Claim.SetDefaultRights(dbUser.Id);
                    EmailVerificationMail.Send(dbUser, emailToken);
                    return true;
                }
                return false;
            }
            catch (Exception exc) { Log.Error(exc); throw; }

        }
        [WebMethod]
        public bool ResendValidationEmail(string email)
        {
            try
            {
                bool sent = false;
                var user = Model.TaggerUser.LoadByEmail(email);
                if (user != null)
                {
                    if (!user.EmailVerified)
                    {
                        string emailToken = user.ResetValidationToken();
                        EmailVerificationMail.Send(user, emailToken);
                        sent = true;
                    }
                    else // We don't send a validation link to an already validated account. Send a Lost Password mail instead.
                        LostPassword(email);
                }
                return sent;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public AjaxUser VerifyEmail(string emailToken)
        {
            try
            {
                Model.TaggerUser user;
                bool validated = Model.TaggerUser.ValidateEmail(emailToken, out user);
                if (validated)
                {
                    // A verified email also logs the user in:
                    _Token = user.GenerateToken(_DeviceId);
                    return AjaxUser.FromTaggerUser(user);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void LogOut()
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                    user.LogOut(_DeviceId);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public AjaxClaim[] GetAllClaims()
        {
            try
            {
                string[] moduleNames = Claim.GetAllClaims();
                return moduleNames.Select(moduleName => new AjaxClaim
                {
                    Name = "module",
                    Value = moduleName
                }).ToArray();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public object GetRights()
        {
            try
            {
                var user = _GetUser();
                if (user == null || !user.IsAdmin)
                    return null;

                return new
                {
                    Claims = GetAllClaims(),
                    Users = Model.TaggerUser.GetAll(true).Select(u=> AjaxUser.FromTaggerUser(u, false))
                };
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public bool SetRight(string userId, string claim, bool granted)
        {
            try
            {
                var user = _GetUser();

                if (user == null || !user.IsAdmin || string.IsNullOrEmpty(userId))
                    return false;

                return Model.Claim.SetModule(userId, claim, granted);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        /// <summary>
        /// Returns the current device ID or generates a new one
        /// </summary>
        private string _DeviceId
        {
            get
            {
                string deviceId;
                var deviceCookie = HttpContext.Current.Request.Cookies["deviceId"];
                if (deviceCookie != null)
                    deviceId = deviceCookie.Value;
                else
                {
                    // No request cookie, check if we already added one in the response
                    if (HttpContext.Current.Response.Cookies.AllKeys.Contains("deviceId"))
                        deviceId = HttpContext.Current.Response.Cookies["deviceId"].Value;
                    else
                    {
                        // Create a new device ID and store it in a cookie
                        deviceId = Model.TaggerUser.GenerateDeviceId();
                        _DeviceId = deviceId;
                    }
                }

                return deviceId;
            }
            set
            {
                var deviceCookie = new HttpCookie("deviceId", value);
                deviceCookie.Expires = DateTime.UtcNow.AddYears(1);
                HttpContext.Current.Response.SetCookie(deviceCookie);
            }
        }
        private string _Token
        {
            get
            {
                var tokenCookie = HttpContext.Current.Request.Cookies["token"];
                if (tokenCookie != null)
                    return tokenCookie.Value;
                return null;
            }
            set
            {
                var cookie = new HttpCookie("token", value);
                if (value == null)
                    cookie.Expires = DateTime.UtcNow.AddDays(-1);
                else
                    cookie.Expires = DateTime.UtcNow.AddYears(1);
                HttpContext.Current.Response.SetCookie(cookie);
            }
        }
        private Model.TaggerUser _GetUser()
        {
            if (_Token != null)
                return Model.TaggerUser.GetByToken(_DeviceId, _Token);
            return null;
        }
        private string _GetUserId()
        {
            var user = _GetUser();
            return user != null ? user.Id : null;
        }
        #endregion

        #region Spot library
        [WebMethod]
        public List<SampleStatus> UpdateSampleStatuses(List<string> sampleIds)
        {
            try
            {
                var user = _GetUser();
                return SpotUpload.UpdateSampleStatuses(user, sampleIds);

            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public object GetMySamples(int pageSize, int pageNum, string sortColumn, bool ascending, AjaxSpotFilter filter)
        {
            var user = _GetUser();
            if (!user.IsGrantedSpotLibrary())
                return null;

            var channelIds = filter.channelIds != null ? filter.channelIds.Select(id => Guid.Parse(id)).ToList() : null;

            try
            {
                int totalCount;
                var songs = Model.Song.GetUserSongs(user, pageSize, pageNum, sortColumn, ascending,
                     channelIds,
                     DateUtility.GetDate(filter.dateFrom),
                     DateUtility.GetDate(filter.dateTo),
                     filter.name, filter.brand, filter.category, filter.advertiser, filter.songStatuses,
                     out totalCount);

                var spots = SongHelper.Sanitize(songs, user)
                     .Select(s => new AjaxSpot(s))
                     .ToArray();

                return new
                {
                    totalCount,
                    spots
                };
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }


        [WebMethod]
        public void MoveScannedSpotsToSpotLibrary()
        {
            try
            {
                var user = _GetUser();
                if (user.IsGrantedSpotLibrary() || user.IsGrantedSpotUpload())
                {
                    Song.SetUploadedStatusToUserScannedSpots(user.Id);
                }
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public List<Service.AjaxSpot> GetUserSpots(string term, bool onlyUploaded = false)
        {
            var user = _GetUser();
            if (!user.IsGrantedSpotLibrary())
            {
                return null;
            }

            try
            {
                var songs = Model.Song.GetUserSpots(user, term, onlyUploaded);
                return songs;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }

        }

        [WebMethod]
        public List<string> SuggestMySampleDescription(string setName, string search)
        {
            var user = _GetUser();
            int max = 12;
            switch (setName)
            {
                case "brand":
                    return Model.Song.SuggestBrands(user.Id, search, max);
                case "category":
                    return Model.Song.SuggestCategories(user.Id, search, max);
                case "advertiser":
                    return Model.Song.SuggestAdvertisers(user.Id, search, max);
                default:
                    return new List<string>();
            }
        }

        [WebMethod]
        public bool UpdateMySample(string sampleId, string name, string brand, string category, string advertiser)
        {
            var user = _GetUser();
            Guid songId;
            try
            {
                if (user.IsGrantedSpotLibrary() && Guid.TryParse(sampleId, out songId))
                {
                    Model.Song.UpdateDescription(songId, name, brand, category, advertiser);
                    return true;
                }
            }
            catch (Exception ex) { Log.Error(ex.ToString()); }
            return false;
        }
        [WebMethod]
        public bool DeleteMySample(string sampleId)
        {
            var user = _GetUser();
            if (user.IsGrantedSpotLibrary())
            {
                try
                {
                    Guid songId;
                    if (Guid.TryParse(sampleId, out songId))
                    {
                        Model.Song.DeleteLogically(songId);
                        return true;
                    }
                }
                catch (Exception ex) { Log.Error(ex.ToString()); }
            }

            return false;
        }
        #endregion

        #region Tagger
        [WebMethod]
        public AjaxSample LoadNext(string filter)
        {
            var user = _GetUser();
            if (user.IsGrantedTagger())
            {
                var taggerSong = Model.TaggerSong.GetNext(user.Id, filter);
                if (taggerSong != null)
                    return new AjaxSample(taggerSong);
            }
            return null;
        }
        [WebMethod]
        public string[] SearchSamples(string filter)
        {
            var user = _GetUser();
            if (user.IsGrantedTagger())
            {
                int max = 10;
                return Model.TaggerSong.GetUntagged(user.Id, filter, max);
            }
            return null;
        }
        [WebMethod]
        public Model.TaggerVote.Statistics GetVoteStatistics(string filter)
        {
            var user = _GetUser();
            if (user.IsGrantedTagger())
            {
                var stats = Model.TaggerVote.GetStatistics(user.Id, filter);
                return stats;
            }
            return null;
        }
        [WebMethod]
        public object Vote(int songId, IdName[] tags, IdName company, IdName industry, IdName category, string productId, string filter)
        {
            var user = _GetUser();
            if (user.IsGrantedTagger())
            {
                var productGuid = Guid.Parse(productId);
                foreach (var tag in tags)
                    Model.TaggerTag.LoadOrCreate(tag, user.Id);
                Model.TaggerCompany.LoadOrCreate(company);
                Model.TaggerIndustry.LoadOrCreate(industry);
                Model.TaggerCategory.LoadOrCreate(category);
                Model.TaggerVote.Vote(
                     songId,
                     tags.Select(tag => tag.Id.Value).ToArray(),
                     company != null ? company.Id : null,
                     industry != null ? industry.Id : null,
                     category != null ? category.Id : null,
                     productGuid,
                     user.Id);
                return LoadNext(filter);
            }
            return null;
        }
        [WebMethod]
        public AjaxSample Skip(int songId, int reason, string filter)
        {
            var user = _GetUser();
            if (user.IsGrantedTagger())
            {
                Model.TaggerVote.SkipSong(songId, (Model.TaggerVote.SkipReason)reason, user.Id);
                return LoadNext(filter);
            }
            return null;
        }
        #endregion

        #region Tagger Lab
        [WebMethod]
        public string[] SuggestLabel(string setName, string search)
        {
            return new Model.LabelSet(setName).GetMatches(search).ToArray();
        }
        [WebMethod]
        public object PickSongToTag()
        {
            try
            {
                var user = _GetUser();
                if (user.IsGrantedTaggerLab())
                {
                    var adTagging = Model.AdTagging.PickNotTagged(user.Id, Model.OneAd.SchemaVersion);
                    var stats = Model.AdTagging.GetStatistics(user.Id, Model.OneAd.SchemaVersion);

                    return new
                    {
                        FilePath = Model.TaggerSong.GetMp3Url(adTagging.PksId),
                        AdTagging = adTagging,
                        Statistics = stats
                    };
                }
            }
            catch (Exception ex)
            {
                _SendExeptionByMail("PickSongToTag", ex);
            }
            return null;
        }
        [WebMethod]
        public void SaveAdTagging(Model.AdTagging adTagging)
        {
            try
            {
                var user = _GetUser();
                if (user.IsGrantedTaggerLab())
                {
                    var labels = adTagging.OneAd.GetLabels();
                    foreach (var label in labels)
                        new Model.LabelSet(label.Item1).InsertIfNew(label.Item2);

                    adTagging.Save();
                }
            }
            catch (Exception ex)
            {
                _SendExeptionByMail("SaveAdTagging", ex);
            }
        }
        private void _SendExeptionByMail(string failingFunctionName, Exception ex)
        {
            var config = Model.Mailer.Configuration.Get();

            var msg = new System.Net.Mail.MailMessage();
            msg.From = new System.Net.Mail.MailAddress(config.DefaultFromEmail, config.DefaultFromName);
            msg.To.Add(new System.Net.Mail.MailAddress(config.DefaultBccEmail));
            msg.Subject = failingFunctionName + " Exception";
            msg.Body = ex.ToString();
            msg.IsBodyHtml = false;
            Model.Mailer.Send(msg);
        }
        #endregion

        #region Word Cut
        [WebMethod]
        public AjaxWordCut PickWordToCut()
        {
            var user = _GetUser();
            if (user.IsGrantedWordCut())
            {
                var wordCut = Model.WordCut.PickNext(user.Id);
                return new AjaxWordCut(wordCut);
            }
            return null;
        }
        #endregion

        #region Matcher
        [WebMethod]
        public AjaxSampleMatch PickNextToMatch(string date)
        {
            var user = _GetUser();
            if (user.IsGrantedMatcher())
            {
                var oDate = DateUtility.GetDate(date);
                var recentMatches = oDate.HasValue ?
                     Model.SampleMatch.GetMatchesByDate(oDate.Value).Randomize() :
                     Model.SampleMatch.GetRecentMatches(100).Randomize();
                int queueLength = oDate.HasValue ?
                     Model.SampleMatch.CountMatchesByDate(oDate.Value) :
                     Model.SampleMatch.CountUncommentedMatches();
                foreach (var pksid in recentMatches)
                {
                    var sampleMatch = Model.SampleMatch.Get(pksid);
                    if (sampleMatch.Matches.Any())
                        return new AjaxSampleMatch(sampleMatch, queueLength);
                    else
                        Model.SampleMatch.SetComment(pksid, masterPksId: null, userId: null, comment: "No matching samples", analyzeDuration: null);
                }
            }
            return null;
        }

        [WebMethod]
        public AjaxSampleMatch CommentAndPickNextToMatch(string pksid, string masterPksid, string comment, int analyzeDuration, string date)
        {
            var user = _GetUser();
            if (user.IsGrantedMatcher())
            {
                Model.SampleMatch.SetComment(pksid, masterPksid, user.Id, comment, analyzeDuration);
                return PickNextToMatch(date);
            }
            return null;
        }

        [WebMethod]
        public AjaxSampleMatch PickThisToMatch(string pksid)
        {
            var user = _GetUser();
            if (user.IsGrantedMatcher())
            {
                var sampleMatch = Model.SampleMatch.Get(pksid);
                return new AjaxSampleMatch(sampleMatch, Model.SampleMatch.CountUncommentedMatches());
            }
            return null;
        }
        #endregion

        #region Playout Map
        [WebMethod]
        public List<string[]> GetCountries()
        {
            return Model.Localization.GetCountries()
                 .Select(c => new string[]
              {
                    c.TwoLetterISORegionName,
                    c.EnglishName
              })
                 .ToList();
        }
        [WebMethod]
        public List<AjaxChannel> GetChannelsAndCityCount()
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedPlayoutMap())
            {
                var channels = Model.Channel.GetAll();
                var countByChannel = Model.City.CountByChannel();
                return channels.Select(channel => new AjaxChannel(channel)
                {
                    CityCount = countByChannel.ContainsKey(channel.Id) ? countByChannel[channel.Id] : 0
                }).ToList();
            }
            return null;
        }
        [WebMethod]
        public List<long> GetChannelCities(Guid channelId)
        {
            return Model.City.GetIdsByChannel(channelId);
        }
        [WebMethod]
        public object GetChannelsCities()
        {
            return Model.City.GetIdsByChannel()
                 .Select(cc => new
                 {
                     ChannelId = cc.Item1,
                     CityIds = cc.Item2
                 })
                 .ToList();
        }
        [WebMethod]
        public bool AddChannelCity(Guid channelId, long cityId)
        {
            var user = _GetUser();
            if (user.IsGrantedPlayoutMap())
                return Model.City.AddToChannel(channelId, cityId);
            return false;
        }
        [WebMethod]
        public bool RemoveChannelCity(Guid channelId, long cityId)
        {
            var user = _GetUser();
            if (user.IsGrantedPlayoutMap())
                return Model.City.RemoveFromChannel(channelId, cityId);
            return false;
        }
        [WebMethod]
        public int ImportCountryCities(string countryCode)
        {
            var user = _GetUser();
            if (user.IsGrantedPlayoutMap())
            {
                var cities = new Model.OverpassQuery().GetCountryCities(countryCode);
                cities.ForEach(p => p.Save());
                return cities.Count;
            }
            return 0;
        }
        [WebMethod]
        public List<Model.City> GetCities(string countryCode)
        {
            if (countryCode == null)
                return Model.City.GetAll();
            if (Model.Localization.GetCountries().Any(c => c.TwoLetterISORegionName == countryCode))
                return Model.City.GetByCountry(countryCode);
            return null;
        }
        [WebMethod]
        public List<Model.Playout> GetCurrentPlayouts(int lastSeconds)
        {
            var offset = new DateTime(2016, 02, 22) - new DateTime(2015, 11, 27); // To look at a period when there was some data
            DateTime start = DateTime.Now - offset;
            DateTime end = start.AddSeconds(30);
            return Model.Playout.GetRecent(start, end);
        }
        #endregion

        #region Tag Manager
        [WebMethod]
        public object GetTaggedSamples(int tagId)
        {
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                var tag = Model.TaggerTag.Get(tagId);
                if (tag != null)
                {
                    return new
                    {
                        samples = Model.TaggerSong
                             .GetTagged(tagId)
                             .Select(s => new AjaxSample(s))
                             .ToArray(),
                        creator = tag.CreatorId != null ? Model.TaggerUser.Get(tag.CreatorId).Email : null
                    };
                }
            }
            return null;
        }
        [WebMethod]
        public IdName[] GetTags(string q)
        {
            return Model.TaggerTag
                 .Find(q)
                 .Select(tag =>
                    new IdName
                    {
                        Id = tag.Id,
                        Name = tag.Name
                    })
                 .ToArray();
        }
        [WebMethod]
        public AjaxTag[] GetTagsUse(string prefix, string q, bool? withBrands, bool? withCompanies)
        {
            return Model.TaggerTag
                 .FindWithUsage(prefix, q)
                 .Where(tagUsage =>
                    (!withBrands.HasValue || withBrands.Value == tagUsage.BrandCount > 0) &&
                    (!withCompanies.HasValue || withCompanies.Value == tagUsage.CompanyCount > 0)
                 )
                 .OrderBy(b => b.Tag.Name)
                 .Take(300)
                 .Select(tagUsage => new AjaxTag(tagUsage))
                 .ToArray();
        }
        [WebMethod]
        public Model.TaggerTag.TagStatistics GetTagStatistics()
        {
            var stats = Model.TaggerTag.GetStatistics();
            return stats;
        }
        [WebMethod]
        public bool MergeTags(int masterId, int[] slaveIds, string name)
        {
            var res = false;
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                foreach (int slaveId in slaveIds)
                    res |= Model.TaggerTag.Merge(user, masterId, slaveId, name);
            }
            return res;
        }
        [WebMethod]
        public AjaxTag SplitTags(int masterId, string name1, string name2)
        {
            AjaxTag res = null;
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                var slaveTag = Model.TaggerTag.Split(user, masterId, name1, name2);
                res = new AjaxTag(Model.TaggerTag.GetUsage(slaveTag.Id));
            }
            return res;
        }
        #endregion

        #region Tag attributes
        [WebMethod]
        public AjaxTagAttribute[] SuggestTagAttributes(string q)
        {
            var user = _GetUser();
            if (user.IsGrantedTagger())
            {
                int max = 25;
                var attributes = new List<AjaxTagAttribute>();

                var taggerBrands = Model.TaggerBrand.Find(q).Select(b => new AjaxTagAttribute(b));
                attributes.AddRange(_getStartingWithFirst(taggerBrands, q, 10));

                var taggerCompanies = Model.TaggerCompany.Find(q).Select(c => new AjaxTagAttribute(c));
                attributes.AddRange(_getStartingWithFirst(taggerCompanies, q, 10));

                return attributes.Take(max).OrderBy(a => a.Name).ToArray();
            }
            return null;
        }
        private List<AjaxTagAttribute> _getStartingWithFirst(IEnumerable<AjaxTagAttribute> foundAttributes, string q, int max)
        {
            var attributes = foundAttributes.Where(c => c.Name.StartsWith(q, StringComparison.InvariantCultureIgnoreCase)).Take(max).ToList();

            if (attributes.Count < max)
            {
                var notStartingWith = foundAttributes.Where(c => !c.Name.StartsWith(q, StringComparison.InvariantCultureIgnoreCase)).Take(max).ToList();
                attributes.AddRange(notStartingWith);
            }
            return attributes.Take(max).ToList();
        }
        [WebMethod]
        public AjaxTagAttribute[] GetTagAttributes(int[] tagIds)
        {
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                var attributes = new List<Model.TaggerAttribute>();
                if (tagIds.Any())
                {
                    var attributeLists = tagIds.Select(tagId => Model.TaggerAttribute.Get(tagId)).ToList();
                    attributes.AddRange(attributeLists.First());
                    foreach (var list in attributeLists.Skip(1))
                    {
                        attributes = attributes // Intersection of both lists
                             .Join(list, a1 => a1.Id, a2 => a2.Id, (a1, a2) => a1)
                             .ToList();
                    }
                }
                return attributes
                     .Select(attribute => new AjaxTagAttribute(attribute))
                     .ToArray();
            }
            return null;
        }
        [WebMethod]
        public void AddTagAttribute(int[] tagIds, AjaxTagAttribute attribute)
        {
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                var type = attribute.Type.ToEnum<Model.TaggerAttribute.AttributeType>();
                foreach (int tagId in tagIds)
                    Model.TaggerAttribute.Add(tagId, type, Guid.Parse(attribute.Guid));
            }
        }
        [WebMethod]
        public void RemoveTagAttribute(int[] tagIds, AjaxTagAttribute attribute)
        {
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                var type = attribute.Type.ToEnum<Model.TaggerAttribute.AttributeType>();
                foreach (int tagId in tagIds)
                    Model.TaggerAttribute.Remove(tagId, type, Guid.Parse(attribute.Guid));
            }
        }
        [WebMethod]
        public AjaxTagAttribute CreateTagAttribute(AjaxTagAttribute attribute)
        {
            var user = _GetUser();
            if (user.IsGrantedTagManager())
            {
                switch (attribute.Type.ToEnum<Model.TaggerAttribute.AttributeType>())
                {
                    case Model.TaggerAttribute.AttributeType.Brand:
                        attribute = new AjaxTagAttribute(Model.TaggerBrand.Create(user, attribute.Name));
                        break;
                    case Model.TaggerAttribute.AttributeType.Company:
                        attribute = new AjaxTagAttribute(Model.TaggerCompany.Create(user, attribute.Name));
                        break;
                }
            }
            return attribute;
        }
        #endregion

        #region Transcripter
        [WebMethod]
        public AjaxTranscript LoadNextTranscript(bool training, bool review)
        {
            var user = _GetUser();

            try
            {
                Model.Transcript next = null;
                if (user != null)
                {
					Log.Info("Getting next transcript for user: " + user.Id);
                    if (training && user.IsGrantedTranscriptTraining())
                        next = Model.Transcript.GetTraining(user.Id);
                    else if (review && user.IsGrantedTranscriptReview())
                        next = Model.Transcript.GetReview(user.Id);
                    else
                        next = Model.Transcript.GetNextQueued(user.Id, Transcript.Configuration.Get().FilterByUserDomain);
                }

                if (next != null)
                {
					Log.Info("LoadNextTranscript: Found song for transcribing: " + next.SongId + " " + next.SongTitle);
                    Model.TranscriptConcurrency.StartTranscribing(user.Id, next.SongId, training, review);
                    return new AjaxTranscript(next);
                }
            }
            catch (Exception ex)
            {
                Log.Error("LoadNextTranscript: user " + user.Name + ", " + ex.ToString());
            }

            return null;
        }
        [WebMethod]
        public void FixMissingFullText()
        {
            Model.Transcript.FixMissingFullText();
        }
        [WebMethod]
        public AjaxTranscript SaveTranscript(AjaxTranscript transcript)
        {
            var user = _GetUser();
            if (user.IsGrantedTranscript())
            {
                try
                {
                    var t = transcript.ToModel(user.Id);

                    var sbt = Model.TranscriptConcurrency.EndTranscribing(user.Id, t.SongId);

                    if (sbt != null &&
                         sbt.Training == t.Training && sbt.Review == t.ReviewOf.HasValue) // Ensure training and review_of flags remained untouched client-side
                    {
                        var config = Model.Transcript.Configuration.Get();

                        t.Insert();

                        if (!t.Training && config.AutoValidateFinishedTranscripts)
                            Model.Transcript.SaveFullText(t.Id, t.GetTranscriptPartsFullText(), true, Model.Transcript.StatusEnum.Corrected, false);

                        if (!t.Training && config.SendMailOnNewTranscript)
                        {
                            decimal performance = t.GetPerformance().Value;
                            string sampleUrl = string.Format("<a href=\"{0}\">{0}</a>", Settings.Get("Song", "SampleRootUrl") + "view.asp?v=" + t.PksId);

                            Func<string, string> _replaceTags = (string s) => s
                                 .Replace("[transcriptDateTime]", Model.Localization.NowLocal.ToString())
                                 .Replace("[transcriberName]", user.DisplayName)
                                 .Replace("[sampleUrl]", sampleUrl)
                                 .Replace("[transcriptText]", t.GetTranscriptPartsFullText())
                                 .Replace("[performance]", performance.ToString("0.0"))
                                 .Replace("[performanceGrade]", Model.Transcript.GetPerformanceGrade(performance).ToString());

                            Model.Mailer.Send(
                                 config.SendMailOnNewTranscriptTo,
                                 _replaceTags(config.TranscriptDoneNotificationSubject),
                                 _replaceTags(config.TranscriptDoneNotificationBody),
                                 isHtml: true);
                        }
                    }
                    else
                        Log.Warn(string.Format("SaveTranscript TranscriptConcurrency: sample {0} has expired, user {1}", t.SongId, user.Name));
                }
                catch (Exception ex)
                {
                    Log.Error("SaveTranscript: user " + user.Name + ", " + ex.ToString());
                }
            }

            return LoadNextTranscript(transcript.Training, transcript.ReviewOf.HasValue);
        }
        [WebMethod]
        public int GetTranscriptQueueLength(bool training, bool review)
        {
            int queueLength = 0;
            var user = _GetUser();
            if (user != null)
            {
                if (training && user.IsGrantedTranscriptTraining())
                    queueLength = Model.Transcript.GetTrainingQueueLength(user.Id);
                else if (review && user.IsGrantedTranscriptReview())
                    queueLength = Model.Transcript.GetReviewQueueLength(user.Id);
                else
                    queueLength = Model.Transcript.GetQueueLength(user.Id,Transcript.Configuration.Get().FilterByUserDomain);

                if (user.Email == "debash1234@gmail.com")
                    Log.Info("Debbie's queue: " + queueLength);
            }

            return queueLength;
        }
        [WebMethod]
        public void RestartTraining()
        {
            var user = _GetUser();
            if (user.IsGrantedTranscript())
                Model.Transcript.RestartTraining(user.Id);
        }
        [WebMethod]
        public string[] NotifyTranscribers(string songId)
        {
            // TODO: add security to avoid misuse of notification

            // Ignoring songId parameter for now

            return Model.Notificator.SendTranscriptAvailable()
                 .Select(user => user.Email)
                 .ToArray();
        }
        #endregion

        #region Transcript statistics
        [WebMethod]
        public AjaxTranscriptStatistic[] GetTranscriptStatistics(string userId, string dateStart, string dateEnd)
        {
            var user = _GetUser();
            List<Model.TranscriptStatistic> statistics = null;

            if (user.IsGrantedTranscriptStats())
            {
                if (string.IsNullOrEmpty(userId))
                {
                    if (string.IsNullOrEmpty(dateStart) || string.IsNullOrEmpty(dateEnd))
                        statistics = Model.Transcript.GetStatistics();
                    else
                        statistics = Model.Transcript.GetStatistics(DateTime.Parse(dateStart), DateTime.Parse(dateEnd));
                }
                else
                {
                    if (string.IsNullOrEmpty(dateStart))
                        statistics = Model.Transcript.GetStatistics(userId);
                    else
                    {
                        if (string.IsNullOrEmpty(dateEnd))
                            statistics = Model.Transcript.GetStatistics(userId, DateTime.Parse(dateStart));
                        else
                            statistics = Model.Transcript.GetStatistics(userId, DateTime.Parse(dateStart), DateTime.Parse(dateEnd));
                    }
                }
            }
            return statistics == null ? null : statistics.Select(s => new AjaxTranscriptStatistic(s)).ToArray();
        }
        [WebMethod]
        public object GetTranscriptStatisticsChart()
        {
            var user = _GetUser();
            if (user.IsGrantedTranscript())
            {
                int statisticsLengthInDays = 30;
                DateTime dateStart = DateTime.UtcNow.AddDays(-statisticsLengthInDays + 1);
                return Model.Transcript.GetStatistics(user.Id, dateStart, statisticsLengthInDays).ToArray();
            }
            return null;
        }
        [WebMethod]
        public AjaxTranscriptEarning[] GetTranscriptEarnings()
        {
            var user = _GetUser();
            if (user.IsGrantedTranscript())
            {
                var lastFullMonth = new mhr.Period(mhr.PeriodKind.LastFullMonth, DateTime.UtcNow);
                var currentMonth = new mhr.Period(mhr.PeriodKind.MonthToDate, DateTime.UtcNow);
                currentMonth.CurrentEnd = DateTime.UtcNow.Date.AddDays(1); // For the current month to date, include today
                var currentWeek = new mhr.Period(mhr.PeriodKind.WeekToDate, DateTime.UtcNow);
                currentWeek.CurrentEnd = DateTime.UtcNow.Date.AddDays(1); // For the current week to date, include today
                var lastFullWeek = new mhr.Period(mhr.PeriodKind.LastFullWeek, DateTime.UtcNow);
                var lastFullWeek2 = new mhr.Period(mhr.PeriodKind.LastFullWeek, DateTime.UtcNow.AddDays(-7));
                var lastFullWeek3 = new mhr.Period(mhr.PeriodKind.LastFullWeek, DateTime.UtcNow.AddDays(-14));

                var stats = Model.Transcript.GetStatistics(user.Id, lastFullMonth.CurrentStart, currentMonth.CurrentEnd);

                return new AjaxTranscriptEarning[]
                {
                    new AjaxTranscriptEarning(currentMonth, stats),
                    new AjaxTranscriptEarning(lastFullMonth, stats),

                    new AjaxTranscriptEarning(currentWeek, stats),
                    new AjaxTranscriptEarning(lastFullWeek, stats),
                    new AjaxTranscriptEarning(lastFullWeek2, stats),
                    new AjaxTranscriptEarning(lastFullWeek3, stats)
                };
            }
            return null;
        }
        #endregion

        #region Transcript manager
        [WebMethod]
        public object GetDistinctFilters()
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                return new
                {
                    Products = Model.Product.GetAll().OrderBy(p => p.Name).Select(p => p.Name).ToArray(),
                    Categories = Model.Category.GetAll().OrderBy(c => c.Name).Select(c => c.Name).ToArray(),
                    Companies = Model.Company.GetAll().OrderBy(c => c.Name).Select(c => c.Name).ToArray(),
                    Brands = Model.Brand.GetAll().OrderBy(b => b.Name).Select(b => b.Name).ToArray(),
                };
            }
            return null;
        }
        [WebMethod]
        public object GetSamples(int pageNum, int pageSize, string sortColumn, bool ascending, AjaxTranscriptFilter filter)
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                pageNum = Math.Max(pageNum, 0);
                pageSize = Math.Max(pageSize, 10);
				var config = Transcript.Configuration.Get();
                int totalCount;
                var samples = Model.Sample.GetFiltered(
                     pageNum,
                     pageSize,
                     sortColumn, ascending,
                     DateUtility.GetDate(filter.startDate), DateUtility.GetDate(filter.endDate), filter.text,
                     filter.status, filter.product, filter.category, filter.company, filter.brand, config.Userid,
                     out totalCount);
                return new
                {
                    totalCount = totalCount,
                    samples = samples.Select(s => new AjaxMgrSample(s)).ToArray()
                };
            }
            return null;
        }
        [WebMethod]
        public object[] GetTranscripts(string songId)
        {
            var user = _GetUser();
            Guid songGuid;
            if (user.IsGrantedTranscriptManager() && Guid.TryParse(songId, out songGuid))
            {
                var transcripts = Model.Transcript.GetBySong(songGuid)
                     .OrderBy(t => t.EditStart)
                     .Select(t => new
                     {
                         Id = t.Id,
                         Text = t.FullText ?? t.GetTranscriptPartsFullText(),
                         EditStart = t.EditStart.HasValue ? t.EditStart.Value.ToString("o") : null,
                         FilePath = Model.TaggerSong.GetMp3Url(t.PksId),
                         User = t.UserId != null ? Model.TaggerUser.Get(t.UserId).DisplayName : null,
                         EditDuration = t.EditStart.HasValue && t.EditEnd.HasValue ?
                            (int)Math.Round((t.EditEnd.Value - t.EditStart.Value).TotalSeconds) :
                            (int?)null,
                         Performance = t.GetPerformance(),
                         PerformanceGrade = Model.Transcript.GetPerformanceGrade(t.GetPerformance()).ToString(),
                         IsMaster = t.IsMaster,
                         Status = (byte)t.Status
                     })
                     .ToArray();

                return transcripts;
            }
            return null;
        }
        [WebMethod]
        public void SaveFullText(int transcriptId, string fullText, bool asMaster, byte status)
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                if (asMaster)
                    Model.Transcript.SetMaster(transcriptId);
                Model.Transcript.SaveFullText(transcriptId, fullText, asMaster, (Model.Transcript.StatusEnum)status);
            }
        }
        [WebMethod]
        public bool OrderTranscripts(string[] songIds, int quantity, byte priority)
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager() && songIds.Any() && quantity > 0)
            {
                Guid songGuid;
                foreach (string songId in songIds)
                {
                    if (Guid.TryParse(songId, out songGuid))
                        Model.Transcript.AddToQueue(songGuid, quantity, priority);
                }
                System.Threading.Tasks.Task.Factory.StartNew(() => Model.Notificator.SendTranscriptAvailable());
                return true;
            }
            return false;
        }
        [WebMethod]
        public void ClearTranscriptQueue(string[] songIds)
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager() && songIds.Any())
            {
                Model.Transcript.Unqueue(songIds);
            }
        }
        [WebMethod]
        public int SetTranscriptQueuePriorityFiltered(AjaxTranscriptFilter filter, byte priority)
        {
            int set = 0;
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                set = Model.Sample.SetPriority(
                     DateUtility.GetDate(filter.startDate), DateUtility.GetDate(filter.endDate), filter.text,
                     filter.status, filter.product, filter.category, filter.company, filter.brand,
                     priority);
            }
            return set;
        }
        [WebMethod]
        public int SetTranscriptQueuePriority(string[] songIds, byte priority)
        {
            int set = 0;
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                set = Model.Sample.SetPriority(songIds, priority);
            }
            return set;
        }
        [WebMethod]
        public string[] GetTranscribersHavingQueue()
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                return Model.Notificator.GetTranscribersHavingQueue()
                     .Select(tq => tq.Transcriber.DisplayName)
                     .ToArray();
            }
            return new string[] { };
        }
        [WebMethod]
        public bool RemindAwaitingTranscripts()
        {
            var user = _GetUser();
            if (user.IsGrantedTranscriptManager())
            {
                var config = Model.Transcript.Configuration.Get();
                System.Threading.Tasks.Task.Factory.StartNew(() => Model.Notificator.RemindAwaitingTranscripts(config.AwaitingTranscriptsNotificationSubject, config.AwaitingTranscriptsNotificationBody));
                return true;
            }
            return false;
        }
        #endregion

        #region Mailing
        [WebMethod]
        public bool SendMailingTest(string fromDisplayName, string subject, string html)
        {
            var sent = false;

            var user = _GetUser();
            if (user.IsGrantedMailing())
                sent = _SendMailing(user.Email, fromDisplayName, user, subject, html);

            return sent;
        }
        [WebMethod]
        public int SendMailing(string fromDisplayName, string subject, string html, AjaxClaim[] claims)
        {
            int sentCount = 0;
            var user = _GetUser();
            if (user.IsGrantedMailing())
            {
                var recipients = _GetRecipients(claims).Select(Model.TaggerUser.Get);

                foreach (var recipient in recipients)
                {
                    if (_SendMailing(user.Email, fromDisplayName, recipient, subject, html))
                        ++sentCount;
                }
            }
            return sentCount;
        }
        private bool _SendMailing(string fromEmail, string fromDisplayName, Model.TaggerUser recipient, string subject, string html)
        {
            var msg = new System.Net.Mail.MailMessage();
            msg.From = new System.Net.Mail.MailAddress(fromEmail, fromDisplayName);
            msg.To.Add(new System.Net.Mail.MailAddress(recipient.Email, recipient.Name));
            msg.Subject = subject.Replace("[name]", recipient.Name);
            msg.Body = html.Replace("[name]", recipient.Name);
            msg.IsBodyHtml = true;
            return Model.Mailer.Send(msg);
        }
        [WebMethod]
        public string[] GetRecipientEmails(AjaxClaim[] claims)
        {
            return _GetUser().IsGrantedMailing() ?
                 _GetRecipients(claims)
                      .Select(Model.TaggerUser.Get)
                      .Select(u => u.Email)
                      .ToArray() :
                 new string[] { };
        }
        private List<string> _GetRecipients(AjaxClaim[] claims)
        {
            var recipientIds = new List<string>();
            foreach (var claim in claims.Select(c => new Model.Claim { Name = c.Name, Value = c.Value }))
                recipientIds = recipientIds.Union(claim.GetUsersHaving()).ToList();
            return recipientIds;
        }
        #endregion

        #region Price Designer

        [WebMethod]
        public object GetChannelWithProducts(string channelId)
        {
            var user = _GetUser();
            if (user.IsGrantedPriceDesigner())
            {
                return ChannelWithProducts.Get(new Guid(channelId));
            }

            return null;
        }

        [WebMethod]
        public void AddChannelWithProducts(List<PriceDefinition> priceDefs)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    ChannelWithProducts.AddOrUpdate(priceDefs);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }


        #endregion

        #region Channels

        [WebMethod]
        public List<ChannelsWithCoverage> GetChannelsWithCoverage()
        {
            try
            {
                var user = _GetUser();
                if (user.IsGrantedChannels())
                {
                    return ChannelsWithCoverage.Get();
                }

            }
            catch (Exception ex) {
				Log.Error(ex);
			}

            return null;
        }

        #endregion Channels

        #region Feeds

        [WebMethod]
        public object GetFeeds(bool checkNew = true)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return null;
            }

            try
            {
				
                var feeds = Model.Feeds.GetFeeds();
				
				var result = new List<Feed>();
				var master = feeds.FirstOrDefault(f => f.Id == 0);
				if(master != null)
					result.Add(master);
				result.AddRange(feeds.Where(f => f.Id > 0));
				return new { NowInLocalTime = DateTime.Now.AddHours(-1 * Feeds.GetTimeZoneOffset()), feeds = result};
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }

        }

		[WebMethod]
		public int GetCountForFeed(int feedId, DateTime? lastTimeStamp)
		{
			if(lastTimeStamp < DateTime.Now.AddDays(-30))
				lastTimeStamp = DateTime.Now.AddDays(-30);
			return Feeds.GetCountForFeed(feedId, lastTimeStamp) ?? 0;
		}

        [WebMethod]
        public long CreateFeed(string client, bool includeMp3, DateTime? expirationDate/*, IEnumerable<FeedEmails> mailingList*/)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                throw new UnauthorizedAccessException();
            }

            try
            {
                return Model.Feeds.CreateFeed(client, includeMp3, expirationDate/*, mailingList*/);
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public long EditFeed(long filterFeedId, string client, bool includeMp3, DateTime? expirationDate/*, IEnumerable<FeedEmails> mailingList*/)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                throw new UnauthorizedAccessException();
            }

            try
            {
                return Model.Feeds.EditFeed(filterFeedId, client, includeMp3, expirationDate/*, mailingList*/);
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public void DeleteFeed(long id)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return;
            }

            try
            {
                Model.Feeds.DeleteFeed(id);
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

		[WebMethod]
		public void DeleteFeeds(string ids)
		{
			var user = _GetUser();
			if (!user.IsGrantedFeeds())
			{
				return;
			}

			try
			{
				Model.Feeds.DeleteFeeds(ids.Split(',').Select(long.Parse).ToList());
			}
			catch (Exception exc)
			{
				Log.Error(exc);
				throw;
			}
		}


		[WebMethod]
        public object GetFeed(long feedFilterId, DateTime cutOffDate, int pageSize, int pageNum, string sortColumn, bool ascending, AjaxFeedFilter filter, bool checkForNewAds = false)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
                return null;

            try
            {
                int totalCount;
				var feed = Model.Feeds.GetFeed(feedFilterId, cutOffDate, pageSize, pageNum, sortColumn, ascending, filter, user, out totalCount);

				if(checkForNewAds && totalCount > 0)
				{
					Log.Info(string.Format("Check for new ads returned {0} records. User: {1}, Feed id: {2}. Date/time (server): {3}", totalCount, user.Email, feedFilterId, DateTime.Now ));
				}

                return new
                {
                    totalCount,
                    feed,
					nowInLocalTime = DateTime.Now.AddHours(-1 * Feeds.GetTimeZoneOffset(feedFilterId))
				};
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

		[WebMethod]
		public object GetFeedMatchCount(long feedFilterId, DateTime? cutOffDate = null)
		{
			return new
			{
				feedFilterId,
				matchCount = Feeds.GetCountForFeed(feedFilterId, cutOffDate),
				cutOffDate
			};
		}


		[WebMethod]
		public object GetFeedsMatchCount(DateTime? cutOffDate = null)
		{
			var feeds = Feeds.GetFeeds();
			var result = new List<object>();
			foreach (var feed in feeds)
			{
				result.Add(GetFeedMatchCount(feed.Id, cutOffDate));
			}
			return result;
		}

        [WebMethod]
        public void SetFeedFilterTimestamp(long feedFilterId, DateTime clientDate)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
                return;

            try
            {
                Model.Feeds.SetFeedFilterTimestamp(feedFilterId, clientDate);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

		[WebMethod]
		public void SetNowAsFeedFilterTimestamp(long feedFilterId)
		{
			var user = _GetUser();
			if (!user.IsGrantedFeeds())
				return;

			try
			{
				var clientDate = GetClientDateNow(feedFilterId);
				Feeds.SetFeedFilterTimestamp(feedFilterId, clientDate.Value);
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}


		[WebMethod]
        public void GenerateFeedResults(string baseUrl, long feedFilterId, DateTime cutOffDate, AjaxFeedFilter filter)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
                return;
            try
            {
                var context = HttpContext.Current;
                Model.Feeds.GenerateResults(context, baseUrl, feedFilterId, cutOffDate, filter);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public object GetFeedResultSongDetails(string songId, string reportId, string feedFilterId)
        {
            //var user = _GetUser();
            //if (!user.IsGrantedFeeds())
                //return null;
            try
            {
                var songDetails = Model.Feeds.GetFeedResultSongDetails(songId, reportId, feedFilterId);
				long feedId;
				var parsed = long.TryParse(feedFilterId, out feedId);
				var timeZoneOffset = Feeds.GetTimeZoneOffset(parsed ? (long?)feedId : null);
				if (songDetails.Created != null)
					songDetails.Created = songDetails.Created.Value.AddHours(-1 * timeZoneOffset);
				return new {
					songDetails.Advertiser,
					songDetails.Brand,
					songDetails.Category,
					songDetails.Channels,
					Created = songDetails.Created != null ? songDetails.Created.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
					songDetails.Duration,
					songDetails.FullTranscript,
					songDetails.Industry,
					songDetails.Market,
					songDetails.Mp3Url,
					songDetails.Region,
					songDetails.Title,
					songDetails.VideoUrl					
				};
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }



        [WebMethod]
        public object GetFeedFilter(long feedFilterId)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return null;
            }

            try
            {
                var feedFilter = Model.Feeds.GetFeedFilter(feedFilterId);
                return new {
					feedFilter.ClientName,
					feedFilter.Id,
					feedFilter.FilterGroups,
					feedFilter.IncludeMp3,
					feedFilter.MailingList,
					Timestamp = feedFilter.Timestamp != null ? (string) feedFilter.Timestamp.Value.ToString("yyyy-MM-dd HH:mm") :  null,
					LastEmailSent = feedFilter.LastEmailSent != null ? (string)feedFilter.LastEmailSent.Value.ToString("yyyy-MM-dd HH:mm") : null,
					userTimeNow = DateTime.Now.AddHours(-1* Feeds.GetTimeZoneOffset(feedFilterId)).ToString("yyyy-MM-dd HH:mm"),
					expirationDate = feedFilter.ExpirationDate != null ? (string)feedFilter.ExpirationDate.Value.ToString("yyyy-MM-dd HH:mm") : null
				};
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }

        }

        [WebMethod]
        public FeedFilter GetFeedFilterForCurrentUser(int? id=null)
        {
			var feeds = GetFeedFiltersForCurrentUser();
			if (feeds != null && feeds.Count > 0)
				return  id == null ? feeds[0] : feeds.FirstOrDefault(f=>f.Id == id);
			return null;			

        }

		[WebMethod]
		public List<FeedFilter> GetFeedFiltersForCurrentUser()
		{
			var user = _GetUser();
			if (!user.IsGrantedFeeds())
			{
				return null;
			}

			try
			{
				var contact = Contact.GetForUser(user.Id);
				if (contact != null)
				{
					var filters = Feeds.GetFeedFiltersForContact(contact.contact_id);
					if (filters.Count > 0)
						return Feeds.GetFeedFilters(filters.Select(f => f.Id).ToList());
				}
				return null;
			}
			catch (Exception exc)
			{
				Log.Error(exc);
				throw;
			}

		}

		[WebMethod]
		public object GetFeedsForCurrentUser()
		{
			var user = _GetUser();
			if (!user.IsGrantedFeeds())
			{
				return null;
			}

			try
			{
				var contact = Contact.GetForUser(user.Id);
				if (contact != null)
				{
					var filters = Feeds.GetFeedFiltersForContact(contact.contact_id);
					if(filters.Count > 0)
						return Feeds.GetFeedFilters(filters.Select(f => f.Id).ToList()).
							Select(ff=> new  {
								Id = ff.Id,
								ff.ClientName,
								Market = string.Join(",",ff.FilterGroups.SelectMany(fg=>fg.FeedFilterRulesMarkets).Select(r=>r.DisplayName)),
								ff.LastEmailSent,
								ff.Timestamp,
								Domain = string.Join(",", ff.FilterGroups.SelectMany(fg => fg.FeedFilterRulesDomains).Select(r => r.DisplayName))
							}).ToList();
				}
				return null;
			}
			catch (Exception exc)
			{
				Log.Error(exc);
				throw;
			}

		}

		[WebMethod]
        public object GetFeedForReportId(string reportId, int pageSize, int pageNum, string sortColumn, bool ascending, AjaxFeedFilter filter)
        {
           // var user = _GetUser();
            //if (!user.IsGrantedFeeds())
            //    return null;

            try
            {
                int totalCount;
				
				FeedFilter feedFilter = null;
                var feed = Model.Feeds.GetFeedForReportId(reportId, pageSize, pageNum, sortColumn, ascending, filter, out totalCount);
				if (totalCount > 0)
					feedFilter = Feeds.GetFeedFilter(feed[0].FeedFilterId.Value);

                return new
                {
                    totalCount,
                    feed,
					feedFilter
                };
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public FeedFilterRuleGroup GetFilterGroup(long filterGroupId)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return null;
            }

            try
            {
                var filterGroup = Model.Feeds.GetFeedFilterRuleGroup(filterGroupId);
                return filterGroup;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }

        }

        [WebMethod]
        public void DeleteFilterGroup(long filterGroupId)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return;
            }

            try
            {
                Model.Feeds.DeleteFeedFilterRuleGroup(filterGroupId);
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public void SaveFilterGroup(FeedFilterRuleGroup filterGroup)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return;
            }

            try
            {
                Model.Feeds.SaveFeedFilterRuleGroup(filterGroup);
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public List<FeedFilterRule> GetAjaxSuggestion(string term, string forModel)
        {
            switch (forModel)
            {
                case "domains":
                    return GetDomains(term).ToFeedFilterRule();
                case "markets":
                    return GetUserMarkets().ToFeedFilterRule();
                default:
                    return new List<FeedFilterRule>();

            }

        }

        [WebMethod]
        public List<AjaxDomain> GetDomains(string term)
        {
            var user = _GetUser();
            if (!user.IsGrantedFeeds())
            {
                return null;
            }

            try
            {
                var domains = Model.Feeds.GetDomains(term);
                return domains;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }

        }

        [WebMethod]
        public object SendFeedEmail(long feedFilterId, IList<FeedResultItem> songs = null, DateTime? dateTo = null)
        {
            var user = _GetUser();
            if (user.IsGrantedFeeds())
            {
				//Feeds.UpdateSongFeedStatus(songs);
				var clientDate = GetClientDateNow(feedFilterId);
				FeedMail.Send(feedFilterId, user.Id, dateTo);				
				Feeds.SetFeedFilterTimestamp(feedFilterId, clientDate.Value);
				var feed = new Feed
				{
					Id = feedFilterId,
					LastTimestamp = clientDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
				};
				Feeds.UpdateNewMatchCount(feed);
				return new { nowInLocalTime = clientDate.Value.ToString("yyyy-MM-dd HH:mm") };
            }
			return null;
        }

		private DateTime? GetClientDate(DateTime date, long feedFilterId)
		{
			var timeZoneOffset = Feeds.GetTimeZoneOffset(feedFilterId);
			return date.AddHours(-1 * timeZoneOffset);
		}

		private DateTime? GetClientDateNow(long feedFilterId)
		{
			return GetClientDate(DateTime.Now, feedFilterId);
		}

		[WebMethod]
		public void UpdateSongFeedStatus(long feedFilterId, IList<FeedResultItem> songs)
		{
			var user = _GetUser();
			if (user.IsGrantedFeeds())
			{
				var timeZoneOffset = Feeds.GetTimeZoneOffset(feedFilterId);
				var clientDate = DateTime.Now.AddHours(-1 * timeZoneOffset);
				Feeds.UpdateSongFeedStatus(user.Id, clientDate, songs);			
			}
		}

		[WebMethod]
		public object GetFeedReports(long feedFilterId)
		{
			var user = _GetUser();
			if (!user.IsGrantedFeeds())
			{
				return null;
			}

			try
			{
				return Feeds.GetFeedReports(feedFilterId);
					
			}
			catch (Exception exc)
			{
				Log.Error(exc);
				throw;
			}

		}

		#endregion Feeds

		#region Reporting
		[WebMethod]
        public Model.Reporting.DateRange GetDataTimeRange()
        {
            return
                 _GetUser().IsGrantedReporting() ?
                 Model.Reporting.DateRange.GetDataTimeRange() :
                 null;
        }
        [WebMethod]
        public Model.Reporting.Report GetReport(Model.Reporting.Reporter reporter)
        {
            return
                 _GetUser().IsGrantedReporting() ?
                 Model.Reporting.Report.GetReport(reporter) :
                 null;
        }
        [WebMethod]
        public Model.Reporting.Reporter[] GetPredefinedReporters()
        {
            return
                 _GetUser().IsGrantedReporting() ?
                 Model.Reporting.PredefinedReports.GetAll() :
                 null;
        }
        [WebMethod]
        public Guid GetCriteriaValueId(int criteriaId, string name)
        {
            return
                 _GetUser().IsGrantedReporting() ?
                 Model.Reporting.CriteriaInfo.GetCriteriaValueId(criteriaId, name) :
                 Guid.Empty;
        }
        #endregion

        #region Media House reporting
        [WebMethod]
        public List<Model.DayPartSet> GetUserDayParts(bool isV2 = false)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var sets = DayPartSet.GetForUser(user.Id);
                if (!sets.Any())
                {
                    var dayPartSet = new DayPartSet
					{
                        Name = "My Day Parts",
                        UserId = user.Id
                    };
                    dayPartSet.Create();
                    sets.Add(dayPartSet);
                }
                return sets;
            }
            return null;
        }
        [WebMethod]
        public Model.DayPartSet CreateDayPartSet(string name)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var dayPartSets = DayPartSet.GetForUser(user.Id);

                if (string.IsNullOrWhiteSpace(name))
                    name = "Day Part";
                while (dayPartSets.Any(dps => dps.Name == name))
                    name += "-1";

                var dayPartSet = new DayPartSet
                {
                    Name = name,
                    UserId = user.Id
                };
                dayPartSet.Create();
                return dayPartSet;
            }
            return null;
        }
        [WebMethod]
        public bool DeleteDayPartSet(int setId)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var dayPartSet = Model.DayPartSet.GetForUser(user.Id).FirstOrDefault(set => set.Id == setId);
                if (dayPartSet != null)
                    return dayPartSet.Delete();
            }
            return false;
        }
        [WebMethod]
        public Model.DayPart CreateDayPart(int setId, Model.DayPart dayPart)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                using (var conn = Database.Get())
                using (var tran = conn.BeginTransaction())
                {
                    var dayPartSet = Model.DayPartSet.GetForUser(conn, tran, user.Id).FirstOrDefault(set => set.Id == setId);
                    if (dayPartSet != null)
                    {
                        if (string.IsNullOrWhiteSpace(dayPart.Name))
                            dayPart.Name = "Day Part";
                        while (dayPartSet.Parts.Any(p => p.Name == dayPart.Name))
                            dayPart.Name += "-1";
                        dayPartSet.Insert(conn, tran, dayPart);
                        tran.Commit();
                        return dayPart;
                    }
                }
            }
            return null;
        }
        private static object updateDayPartLock = new object();
        [WebMethod]
        public bool UpdateDayPart(Model.DayPart dayPart)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                lock (updateDayPartLock)
                    using (var conn = Database.Get())
                    using (var tran = conn.BeginTransaction())
                    {
                        var dayPartSet = Model.DayPartSet.GetForUser(conn, tran, user.Id).FirstOrDefault(set => set.Parts.Any(part => part.Id == dayPart.Id));
                        if (dayPartSet != null)
                        {
                            if (string.IsNullOrWhiteSpace(dayPart.Name))
                                dayPart.Name = "Day Part";
                            while (dayPartSet.Parts.Any(p => p.Id != dayPart.Id && p.Name == dayPart.Name))
                                dayPart.Name += "-1";

                            var dbPart = dayPartSet.Parts.First(part => part.Id == dayPart.Id);
                            bool updated = dayPartSet.Update(conn, tran, dbPart, dayPart);
                            tran.Commit();
                            return updated;
                        }
                    }
            }
            return false;
        }
        [WebMethod]
        public bool DeleteDayPart(int setId, int partId)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var dayPartSet = Model.DayPartSet.GetForUser(user.Id).FirstOrDefault(set => set.Id == setId);
                if (dayPartSet != null)
                    return dayPartSet.DeletePart(partId);
            }
            return false;
        }
        [WebMethod]
        public IEnumerable<Brand> GetBrandNamesByCriteria(string criteria)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return Brand.SearchBrandNamesByCriteria(criteria);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public List<Advertiser> GetAdvertiserNamesByCriteria(string criteria)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="GetAdvertiserNamesByCriteria"
                {
                    return mhr.RankedAdvertisers.SearchAdvertiserNamesByCriteria(criteria);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public object GetMediaHouses()
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var channels = Model.Channel.GetAll();
                var groups = Model.Group.GetByUser(user.Id);
                var holdings = Model.Holding.GetByUser(user.Id);
                return new
                {
                    Channels = channels,
                    Groups = groups,
                    Holdings = holdings
                };
            }
            return null;
        }
        [WebMethod]
        public Model.Channel[] GetChannels()
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return Model.Channel.GetAll().ToArray();
            return null;
        }
        [WebMethod]
        public Model.Group CreateGroup(Model.Group group)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                group.UserId = user.Id;
                group.Insert();
                return group;
            }
            return null;
        }
        [WebMethod]
        public bool UpdateGroup(Model.Group group)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var groups = Model.Group.GetByUser(user.Id);
                return group.Update(groups.FirstOrDefault(g => g.Id == group.Id && g.UserId == group.UserId));
            }
            return false;
        }
        [WebMethod]
        public bool DeleteGroup(Model.Group group)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var groups = Model.Group.GetByUser(user.Id);
                if (groups.Any(g => g.Id == group.Id && g.UserId == group.UserId))
                    return group.Delete();
            }
            return false;
        }
        [WebMethod]
        public Model.Holding CreateHolding(Model.Holding holding)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                holding.UserId = user.Id;
                holding.Insert();
                return holding;
            }
            return null;
        }
        [WebMethod]
        public bool UpdateHolding(Model.Holding holding)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var holdings = Model.Holding.GetByUser(user.Id);
                return holding.Update(holdings.FirstOrDefault(g => g.Id == holding.Id && g.UserId == holding.UserId));
            }
            return false;
        }
        [WebMethod]
        public bool DeleteHolding(Model.Holding holding)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                var holdings = Model.Holding.GetByUser(user.Id);
                if (holdings.Any(g => g.Id == holding.Id && g.UserId == holding.UserId))
                    return holding.Delete();
            }
            return false;
        }
        [WebMethod]
        public void SetMyHolding(int? holdingId)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
            {
                Model.Holding.SetMine(user.Id, holdingId);
            }
        }
        [WebMethod]
        public Model.BrandvertiserPage GetBrandvertisers(bool brands, bool advertisers, string filter, int pageNum, int pageSize)
        {
            var p = new Model.BrandvertiserPage(brands, advertisers, pageNum, pageSize, filter);
            p.Count();
            p.Load();
            return p;
        }
        [WebMethod]
        public List<Model.Brandvertiser> GetKeyAccounts()
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return Model.KeyAccount.GetByUser(user.Id);
            return null;
        }
        [WebMethod]
        public bool AddKeyAccount(Model.Brandvertiser brandvertiser)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return Model.KeyAccount.Add(user.Id, brandvertiser) == 1;
            return false;
        }
        [WebMethod]
        public bool RemoveKeyAccount(Model.Brandvertiser brandvertiser)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return Model.KeyAccount.Remove(user.Id, brandvertiser);
            return false;
        }
        [WebMethod]
        public Model.Competitor[] GetCompetitors()
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return Model.Competitor.GetAll(user.Id).ToArray();
            return null;
        }
        [WebMethod]
        public bool AddCompetitor(Model.Competitor competitor)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return competitor.Add(user.Id);
            //catch { }; // DB rejects inserting duplicate entries
            return false;
        }
        [WebMethod]
        public bool RemoveCompetitor(Model.Competitor competitor)
        {
            var user = _GetUser();
            if (user != null && user.IsGrantedMediaHouse())
                return competitor.Remove(user.Id);
            return false;
        }
        [WebMethod]
        public mhr.TopChart MediaHouseTopChart(string channelId, mhr.IncludeSet include, mhr.PeriodInfo period, mhr.GroupingValue value)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Top Chart"
                    return new mhr.TopChart(user.Id, Guid.Parse(channelId), include, period, value);
                return null;

            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.SalesLeads MediaHouseSalesLeads(string channelId, mhr.IncludeSet include, mhr.PeriodInfo period, bool lessthan, decimal spent, string industryId)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Sales Leads"
                    return new mhr.SalesLeads(user.Id, Guid.Parse(channelId), include, period, lessthan, spent, industryId);
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }

        }
        [WebMethod]
        public mhr.Brandvertiser MediaHouseBrandvertiser(string id, bool isBrand, string channelId, mhr.IncludeSet include, mhr.PeriodInfo period, mhr.GroupingValue value)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Brandvertiser"
                {
                    var brandvertiser = Model.Brandvertiser.Get(Guid.Parse(id), isBrand);
                    return new mhr.Brandvertiser(brandvertiser, user.Id, Guid.Parse(channelId), include, period, value).LoadDashboard();
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.ActivityByDaypart MediaHouseActivityByDaypart(string channelId, mhr.IncludeSet include, mhr.GroupingValue value, mhr.PeriodInfo period, mhr.DayOfWeekRange dayOfWeekRange, mhr.DayPartType dayPart, bool viewData)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.ActivityByDaypart(user.Id, Guid.Parse(channelId), include, value, period, dayOfWeekRange, dayPart, viewData);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.BrandActivityByWeekday MediaHouseBrandActivityByWeekday(mhr.GroupingValue value, mhr.PeriodInfo period, string industryId, List<Guid> categories, string marketId, int limit)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.BrandActivityByWeekday(user.Id, value, period, industryId, categories, marketId, limit);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.BrandActivityByMediaHouse MediaHouseBrandActivityByMediaHouse(mhr.GroupingValue value, mhr.PeriodInfo period, string industryId, mhr.BrandOrAdvertiser shareBy, List<Guid> categories, string marketId, int limit)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.BrandActivityByMediaHouse(user.Id, value, period, industryId, shareBy, categories, marketId, limit);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.KeyAccountActivity MediaHouseKeyAccountActivity(string channelId, mhr.IncludeSet include, mhr.GroupingValue value, mhr.PeriodInfo period, bool showAllAccounts)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.KeyAccountActivity(user.Id, Guid.Parse(channelId), include, value, period, showAllAccounts);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.ShareOfAdvertisingActivity MediaHouseShareOfAdvertisingActivity(string channelId, mhr.IncludeSet include, mhr.GroupingValue value, mhr.PeriodInfo period, mhr.DayOfWeekRange dayOfWeekRange, mhr.DayPartType dayPart)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.ShareOfAdvertisingActivity(user.Id, Guid.Parse(channelId), include, value, period, dayOfWeekRange, dayPart);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.ShareOfBusiness MediaHouseShareOfBusiness(string channelId, mhr.IncludeSet include, mhr.PeriodInfo period, mhr.DayPartType dayPart)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.ShareOfBusiness(user.Id, Guid.Parse(channelId), include, period, dayPart);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public List<string> MediaHouseRotatingAds(string id, bool isBrand, string channelId, mhr.IncludeSet include, mhr.PeriodInfo period, mhr.GroupingValue value)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Brandvertiser"
                {
                    var brandvertiser = Model.Brandvertiser.Get(Guid.Parse(id), isBrand);
                    return new mhr.Brandvertiser(brandvertiser, user.Id, Guid.Parse(channelId), include, period, value).LoadAdsInRotation();
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.MarketOverview MediaHouseMarketOverview(mhr.PeriodInfo period, bool sortByPreviousPeriod, mhr.GroupingValue value, string by, string marketId)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Market Overview"
                {
                    return new mhr.MarketOverview(user.Id, period, sortByPreviousPeriod, value, by, marketId);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.MarketTrend MediaHouseMarketTrend(mhr.GroupingValue value, string mediaType, string marketId)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Market 12 Month Trend"
                {
                    return new mhr.MarketTrend(user.Id, value, mediaType, marketId);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.AdvertisingActivityTrend MediaHouseAdvertisingActivityTrend(mhr.AdvetiserActivityTrendInfo reportMetadata)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Advertiser Activity Trend"
                    return new mhr.AdvertisingActivityTrend(user.Id, Guid.Parse(reportMetadata.channelId), reportMetadata.include, reportMetadata.period, reportMetadata.timeFrom, reportMetadata.timeTo, reportMetadata.value);
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.AsRunLog MediaHouseAsRunLog(string channelId, mhr.IncludeSet include, DateTime date, string brandOrAdvertiserId, mhr.BrandOrAdvertiser brandOrAdvertiser, bool showDuplicates = false)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="As-Run Log"
                {
                    return new mhr.AsRunLog(user.Id, Guid.Parse(channelId), include, brandOrAdvertiserId, brandOrAdvertiser, date, showDuplicates);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.RankedAdvertisers MediaHouseRankedAdvertisers(mhr.PeriodInfo period, mhr.GroupingValue value, string marketId)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Ranked Advertisers"
                    return new mhr.RankedAdvertisers(user.Id, value, period, marketId);
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.ShareByBrandOrAdvertiser MediaHouseShareByBrandOrAdvertiser(mhr.PeriodInfo period, string industryId, mhr.GroupingValue value, mhr.BrandOrAdvertiser shareBy, List<Guid> categories, string marketId, int limit)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="Ranked Advertisers"
                    return new mhr.ShareByBrandOrAdvertiser(user.Id, period, industryId, value, shareBy, categories, marketId, limit);
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.AdvertisingSummary MediaHouseAdvertisingSummary(string channelId, mhr.IncludeSet include, mhr.PeriodInfo period, mhr.BrandAdvertiserOrChannel groupBy)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="AdvertitsingSummary"
                {
                    return new mhr.AdvertisingSummary(user.Id, Guid.Parse(channelId), include, period, groupBy);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.AdBlocks MediaHouseAdBlocks(string channelId, mhr.IncludeSet include, DateTime date, mhr.ChannelOrDate channelOrDate, mhr.DayPartType dayPart)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse()) // sub="AdBlocks"
                {
                    return new mhr.AdBlocks(user.Id, Guid.Parse(channelId), include, date, channelOrDate, dayPart);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public mhr.InvestmentTrend MediaHouseInvestmentTrend(mhr.GroupingValue value, string industryId, mhr.Media media, mhr.BrandOrAdvertiser shareBy, List<Guid> categories, string marketId, int limit)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.InvestmentTrend(user.Id, value, industryId, media, shareBy, categories, limit, marketId);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.Clutter MediaHouseClutter(string channelId, mhr.DayPartType dayPart, int adBreakDurationInSeconds, DateTime date)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.Clutter(user.Id, Guid.Parse(channelId), mhr.IncludeSet.None, dayPart, adBreakDurationInSeconds, date);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.CompetitorProximity MediaHouseCompetitorProximity(string channelId, string brandId, DateTime date)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.CompetitorProximity(user.Id, Guid.Parse(channelId), mhr.IncludeSet.None, brandId, date);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public mhr.AdvertisingLogsByBrand MediaHouseAdvertisingLogsByBrand(string channelId, string industryId, mhr.Media media, DateTime date, List<Guid> categories, mhr.BrandOrAdvertiser brandOrAdvertiser)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMediaHouse())
                {
                    return new mhr.AdvertisingLogsByBrand(user.Id, Guid.Parse(channelId), mhr.IncludeSet.None, industryId, media, date, categories, brandOrAdvertiser);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        #endregion

        #region Report view
        [WebMethod]
        public object[] GetAvailableReports()
        {
            var reports = new List<object>();
            var user = _GetUser();
            if (user != null)
            {
                if (user.IsGrantedMediaHouse()) // sub="Top Chart"
                    reports.Add("Top Chart");
            }
            return reports.ToArray();
        }
        [WebMethod]
        public Model.Channel[] GetMyChannels()
        {
            var user = _GetUser();
            if (user != null)
                return Model.Channel.GetByUser(user.Id).ToArray();

            return null;
        }
        #endregion

        #region Audit

        [WebMethod]
        public object MediaHouseAuditLog(IEnumerable<string> channelIds, IEnumerable<string> songIds, DateTime dateFrom, DateTime dateTo, int auditId = 0)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    return new AuditLog(channelIds.Select(Guid.Parse), songIds.Select(Guid.Parse), dateFrom.Date, dateTo.Date, auditId);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public object MediaHouseAuditDetails(IEnumerable<string> channelIds, IEnumerable<string> songIds, DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    return new AuditDetails(channelIds.Select(Guid.Parse), songIds.Select(Guid.Parse), dateFrom.Date, dateTo.Date);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }


        [WebMethod]
        public IEnumerable<Audit> GetUserAudits()
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    return Audit.GetUserAudits(user.Id);
                }

                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public int SaveAudit(Audit audit)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    audit.UserId = user.Id;
					audit.DateFrom = audit.DateFrom.AddHours(Feeds.GetTimeZoneOffset());
					audit.DateTo = audit.DateFrom.AddHours(Feeds.GetTimeZoneOffset());
					return Audit.SaveAudit(audit);
                }

                return 0;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public void DeleteAudit(int auditId)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    var audit = Audit.GetAudit(auditId);
                    if (audit.UserId == user.Id)
                    {
                        Audit.DeleteAudit(auditId);
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Only audit owner is allowed to delete audit");
                    }
                }
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public void UpsertAuditChannelThresholdById(int auditChannelId, double threshold)
        {
            _UpsertAuditChannelThreshold(auditChannelId, threshold);
        }


        [WebMethod]
        public void UpsertAuditChannelThreshold(int auditId, Guid channelId, double threshold)
        {
            var auditChannel = AuditChannel.GetAuditChannel(auditId, channelId);
            _UpsertAuditChannelThreshold(auditChannel.Id, threshold);
        }

        private void _UpsertAuditChannelThreshold(int auditChannelId, double threshold)
        {
            try
            {
                if (threshold < 0.50 || threshold > 1)
                {
                    throw new HttpRequestValidationException(String.Format("threshold value [{0}] is not valid should be between 0.5 and 1", threshold));
                }

                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    AuditChannelThreshold.Upsert(auditChannelId, threshold);
                }
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public IEnumerable<AuditChannelThreshold> GetAuditChannelsThreshold(int auditId)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedAuditing())
                {
                    return AuditChannelThreshold.GetForAudit(auditId);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        #endregion

        #region Settings
        [WebMethod]
        public object GetSettings()
        {
            var user = _GetUser();
            if (user != null)
            {
                return new
                {
                    Global = Model.GlobalConfiguration.Get(user),
                    Mail = user.IsAdmin ? Model.Mailer.Configuration.Get() : new Model.Mailer.Configuration(),
                    Transcript = user.IsAdmin ? Model.Transcript.Configuration.Get() : new Model.Transcript.Configuration()
                };

            }
            return null;
        }
        [WebMethod]
        public object GetAppSettings()
        {
            try
            {
                return Settings.GetModuleSettings(new[] { "ClientSideSettings", "Localization" });
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public object GetUserSettings()
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                {
                    return UserSettings.Get(user.Id);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }
        [WebMethod]
        public object UpdateUserSetting(UserSettingModule module, string key, string value)
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                {
                    UserSettings.Update(user.Id, module, key, value);
                }
                return null;
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                throw;
            }
        }

        [WebMethod]
        public void SaveGlobalSettings(Model.GlobalConfiguration config)
        {
            var user = _GetUser();
            if (user != null && user.IsAdmin)
                config.Save(user);
        }
        [WebMethod]
        public void SaveMailSettings(Model.Mailer.Configuration config)
        {
            var user = _GetUser();
            if (user != null && user.IsAdmin)
                config.Save();
        }
        [WebMethod]
        public void SaveTranscriptSettings(Model.Transcript.Configuration config)
        {
            var user = _GetUser();
            if (user != null && user.IsAdmin)
                config.Save();
        }

		[WebMethod]
		public void UnlockTranscribedSongs()
		{
			TranscriptConcurrency.EndAll();
		}

        [WebMethod]
        public void ClearSettingsCache(Model.Transcript.Configuration config)
        {
            var user = _GetUser();
            if (user != null && user.IsAdmin) CachedSettings.ClearCache();
        }

        [WebMethod]
        public AuditSettings GetAuditSettings()
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    return AuditSettings.Get();
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void SaveAuditSettings(AuditSettings settings)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    AuditSettings.Save(settings);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public IApplicationSettings GetDokazniceSettings()
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    return DokazniceSettings.Get();
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void SaveDokazniceSettings(DokazniceSettings settings)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    DokazniceSettings.Save(settings);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public IApplicationSettings GetAdKSettings()
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    return AdKSettings.Get();
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void SaveAdKSettings(AdKSettings settings)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    AdKSettings.Save(settings);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public FeedSettings GetFeedSettings()
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                {
                    return FeedSettings.Get();
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void SaveFeedSettings(FeedSettings settings)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    FeedSettings.Save(settings);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

		[WebMethod]
		public string GetSetting(string module, string key)
		{
			return Settings.Get(module, key);
		}

        #endregion

        #region common

        [WebMethod]
        public IEnumerable<TaggerIndustry> GetAllIndustries()
        {
            try
            {
                return _GetUser() != null ? TaggerIndustry.GetAll() : null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Category> GetAllCategories()
        {
            try { return _GetUser() != null ? Category.GetAll() : null; }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Advertiser> GetAllAdvertisers()
        {
            try
            {
                return _GetUser() != null ? Advertiser.GetAll() : null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Brand> GetAllBrands()
        {
            try
            {
                return _GetUser() != null ? Brand.GetAll() : null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public string GetRevisionNumber()
        {
            try
            {
                string filePath = String.Format("{0}revision.txt", AppDomain.CurrentDomain.BaseDirectory);
                return File.ReadLines(filePath).FirstOrDefault();
            }
            catch (Exception exc)
            {
                Log.Error(exc);
                return "";
            }
        }


        [WebMethod]
        public object GetProducts()
        {
            try { return _GetUser() != null ? Product.GetAll() : null; }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public void AddProduct(string productName)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    Product.AddProduct(productName);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public object GetPriceDefinitions(Guid channelId, Guid productId)
        {
            try { return _GetUser() != null ? PriceDefinition.Get(channelId, productId) : null; }
            catch (Exception exc) { Log.Error(exc); throw; }
        }




        #endregion

        #region Markets
        [WebMethod]
        public List<Market> GetUserMarkets()
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMarkets())
                {
                    return Market.GetUserMarkets(user.Id);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public void AddMarket(string name)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMarkets())
                {
                    Market.AddMarket(user.Id, name);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public void UpdateMarket(int marketId, string name)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMarkets())
                {
                    Market.UpdateMarket(user.Id, marketId, name);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }


        [WebMethod]
        public void DeleteMarket(int id)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMarkets())
                {
                    Market.DeleteMarket(user.Id, id);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public void ToggleMarketChannel(string channelId, int marketId, bool active)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMarkets())
                {
                    if (active)
                    {
                        Market.AddMarketChannel(channelId, marketId);
                    }
                    else
                    {
                        Market.DeleteMarketChannel(channelId, marketId);
                    }
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public List<MarketChannelsGroup> GetUserMarketChannelsGroup()
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsGrantedMarkets())
                {
                    return MarketChannels.GetUserMarketChannelsGroup(user.Id);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        #endregion

        #region Notes
        [WebMethod]
        public Note GetNoteByKey(string key)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    return Note.GetByKey(key);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public void InserOrUpdateNote(string noteKey, string noteContent)
        {
            try
            {
                var user = _GetUser();
                if (user != null && user.IsAdmin)
                {
                    Note.InsertOrUpdate(noteKey, noteContent, user.Id);
                }
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        #endregion

        #region Price designer 1.1

        [WebMethod]
        public object GetChannelWithPriceDefinitions11(Guid channelId)
        {
            try { return _GetUser() != null ? ChannelWithPriceDefs11.Get(channelId, _GetUserId()) : null; }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public List<PriceDefinition11> SaveChannelPriceDefinitions11(List<PriceDefinition11> priceDefs)
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                {
                    return PriceDefinition11.AddOrUpdate(priceDefs);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }

        [WebMethod]
        public object SaveChannelProductDayParts(List<ProductDayPart> productDayParts, List<string> deleted)
        {
            try
            {
                var user = _GetUser();
                if (user != null)
                {
                    return ProductDayPart.AddOrUpdate(productDayParts, deleted);
                }
                return null;
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        #endregion

        #region DPOC
        [WebMethod]
        public List<Model.Dpoc.HarvestingStat> DpocHarvestingStats()
        {
            try
            {
                return Model.Dpoc.GetHarvestingStats();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.HarvestingStat> DpocHarvestingStatsMis()
        {
            try
            {
                return Model.Dpoc.GetHarvestingStatsMis();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.PromotionDate> DpocPromotionQueue()
        {
            try
            {
                return Model.Dpoc.GetPromotionQueue();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.NotPromotedClip> DpocNotPromotedClips()
        {
            try
            {
                return Model.Dpoc.GetNotPromotedClips();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.ChannelCoverage> DpocCoverageWorst(decimal numdays)
        {
            try
            {
                return Model.Dpoc.GetCoverageWorst(numdays);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public decimal DpocMissingTotal(decimal numdays)
        {
            try
            {
                return Model.Dpoc.GetMissingTotal(numdays);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string DpocCountBadChannels(decimal numdays)
        {
            try
            {
                return Model.Dpoc.GetCountBadChannels(numdays);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.DuplicateDelay> DpocDuplicateDelays()
        {
            try
            {
                return Model.Dpoc.GetDuplicateDelays();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.ChannelStatus> DpocHashingDetails(string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetHashingDetails(whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public Model.Dpoc.HashingStatus DpocHashingStatus(string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetHashingStatus(whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.HarvestingDateStats> DpocHarvestingStatus(string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetHarvestingStatus(whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public Model.Dpoc.HashingStatus DpocCaptureStatus(string whichWeb, string delayMin)
        {
            try
            {
                return Model.Dpoc.GetCaptureStatus(whichWeb, delayMin);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.ChannelStatus> DpocCaptureDetails(string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetCaptureDetails(whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string DpocCaptureActionTimestamp(string channelId)
        {
            try
            {
                return Model.Dpoc.GetCaptureActionTimestamp(channelId);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.ChannelDetails> DpocCaptureChannel(string channel, string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetCaptureChannel(channel, whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.HoleDetails> DpocCaptureHolesChannel(string channel, string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetCaptureHolesChannel(channel, whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public List<Model.Dpoc.DeadChannel> DpocDeadChannels(string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetDeadChannels(whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public Model.Dpoc.DuplicateStatusLate DpocDuplicateStatusLate()
        {
            try
            {
                return Model.Dpoc.GetDuplicateStatusLate();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public Model.Dpoc.ChannelData DpocGetChannelById(string channel, string whichWeb)
        {
            try
            {
                return Model.Dpoc.GetChannelById(channel, whichWeb);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public Model.Dpoc.DuplicateStatusFinished DpocDuplicateStatusFinished()
        {
            try
            {
                return Model.Dpoc.GetDuplicateStatusFinished();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void DpocUpdateChannelAction(string channel, decimal action)
        {
            try
            {
                var user = _GetUser();
                Dpoc.UpdateChannelAction(user.Id, channel, action);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        #endregion

        #region MONITOR
        private static string DataTableToJson(DataTable dt)
        {
            System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            Dictionary<string, object> row;
            foreach (DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row.Add(col.ColumnName, dr[col].ToString());
                }
                rows.Add(row);
            }

            List<string> colTypes = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                colTypes.Add(col.DataType.Name.ToString());
            }

            var res = new { rows, colTypes };
            return serializer.Serialize(res);
        }

        [WebMethod]
        public string MonitorCaptureSummary()
        {
            try
            {
                int recordingType = 4;
                return DataTableToJson(Monitor.GetRecordingsSummary(recordingType));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorHashSummary()
        {
            try
            {
                int recordingType = 0;
                return DataTableToJson(Monitor.GetRecordingsSummary(recordingType));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorRciLookupLimit()
        {
            try
            {
                return Monitor.GetRciLookupLimit();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorCaptureStatusAllChannels()
        {
            try
            {
                int recordingType = 4;
                return DataTableToJson(Monitor.GetRecordingStatusAllChannels(recordingType));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorHashesStatusAllChannels()
        {
            try
            {
                int recordingType = 0;
                return DataTableToJson(Monitor.GetRecordingStatusAllChannels(recordingType));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorGetChannelById(string channel)
        {
            try
            {
                return DataTableToJson(Monitor.GetChannelById(channel));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorCaptureChannel(string channel )
        {
            try
            {
                return DataTableToJson(Monitor.GetCaptureChannel(channel));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorCaptureHolesChannel(string channel)
        {
            try
            {
                return DataTableToJson(Monitor.GetCaptureHolesChannel(channel));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorDuplicateSummaryFinished()
        {
            try
            {
                return DataTableToJson(Monitor.GetDuplicateSummaryFinished());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorDuplicateSummaryLate()
        {
            try
            {
                return DataTableToJson(Monitor.GetDuplicateSummaryLate());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorDuplicatesDetails()
        {
            try
            {
                return DataTableToJson(Monitor.GetDuplicatesDetails());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorDuplicateDetectionReport()
        {
            try
            {
                return DataTableToJson(Monitor.getDuplicateDetectionReport());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorHarvestingSummary()
        {
            try
            {
                int limitDates = 1;
                return DataTableToJson(Monitor.GetHarvestingSummary(limitDates));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorHarvestingStatus()
        {
            try
            {
                return Monitor.GetHarvestingStatus();
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorPromotionSummary()
        {
            try
            {
                return DataTableToJson(Monitor.GetPromotionSummary());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorPromotionDetails()
        {
            try
            {
                return DataTableToJson(Monitor.GetPromotionDetails());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorPromotionQueue()
        {
            try
            {
                return DataTableToJson(Monitor.GetPromotionQueue());
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public void MonitorUpdateRecordingStatus(string channel_id, decimal status, decimal hold, string reason)
        {
            try
            {
                var user = _GetUser();
                Monitor.UpdateRecordingStatus(user.Id, channel_id, status, hold, reason);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorGetChannelStatus(string channel_id)
        {
            try
            {
                return DataTableToJson(Monitor.GetChannelStatus(channel_id));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorCoverageForChannels(string channel_tag)
        {
            try
            {
                return DataTableToJson(Monitor.GetCoverageForChannels(channel_tag));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorWhatClient()
        {
            return Monitor.WhatClient();
        }
        [WebMethod]
        public string MonitorCoverageByChannel(string week, string channel_tag)
        {
            try
            {
                return DataTableToJson(Monitor.GetCoverageByChannel(week, channel_tag));
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        [WebMethod]
        public string MonitorMondayOfWeek(string week)
        {
            try
            {
                return Monitor.GetMondayOfWeek(week);
            }
            catch (Exception exc) { Log.Error(exc); throw; }
        }
        #endregion

        #region OTHER
        public class IdName
        {
            public int? Id;
            public string Name;
        }
        public class AjaxTag : IdName
        {
            public int Songs;
            public int[] Attributes;
            internal protected AjaxTag() { }
            internal protected AjaxTag(Model.TaggerTag.TagUsage tagUsage)
            {
                Id = tagUsage.Tag.Id;
                Name = tagUsage.Tag.Name;
                Attributes = new int[] {
                    tagUsage.SampleCount,
                    tagUsage.BrandCount,
                    tagUsage.CompanyCount
                };
            }
        }
        public class AjaxUser
        {
            public string id;
            public string email;
            public string password;
            public string name;
            public bool isAdmin;
            public string[] granted;
            public bool isEmailVerified;
            public string locale;
            public string timezone;

            public static AjaxUser FromTaggerUser(Model.TaggerUser user, bool useConfig = true)
            {
                if (user != null)
                {
                    var config = useConfig ? Model.GlobalConfiguration.Get(user) : null;
                    return new AjaxUser
                    {
                        id = user.Id,
                        email = user.Email,
                        name = user.Name,
                        isAdmin = user.IsAdmin,
                        isEmailVerified = user.EmailVerified,
                        granted = user.Claims != null ? user.Claims.Select(c=>c.Value).ToArray() : Claim.GetValues(user.Id, "module").ToArray(),
                        locale = config != null ? config.Locale : string.Empty,
                        timezone = config != null ? config.TimeZone : string.Empty
                    };
                }
                return null;
            }
        }
        public class AjaxClaim : IdName
        {
            public string Value;

            public static AjaxClaim FromModel(Model.Claim c)
            {
                return new AjaxClaim { Id = c.Id, Name = c.Name, Value = c.Value };
            }
        }
        public class AjaxSample : IdName
        {
            public string FilePath;
            public AjaxSample() { }
            internal protected AjaxSample(Model.TaggerSong taggerSong)
            {
                Id = taggerSong.Id;
                Name = taggerSong.Title;
                FilePath = Model.TaggerSong.GetMp3Url(taggerSong.PksId);
            }
        }
        public class AjaxSpot : GuidName
        {
            public string Filename;
            public string FileUrl;
            public string Brand;
            public string Category;
            public string Advertiser;
            public decimal Duration;
            public string Created;
            public DateTime? ScannedToDate;

            public AjaxSpot()
            {

            }

            public AjaxSpot(Model.Song s)
                : base(s.Id, s.Title)
            {
                Filename = s.Filename;
                FileUrl = s.GetMp3Url();
                Brand = s.Brand;
                Category = s.Album;
                Advertiser = s.Performer;
                Duration = s.Duration;
                Created = s.Created.HasValue ? s.Created.Value.ToString("s") : null;
                ScannedToDate = s.ScannedToDate;
            }
        }
        public class AjaxSpotFilter
        {
            public string name;
            public string brand;
            public string category;
            public string advertiser;
            public string[] channelIds;
            public string dateFrom;
            public string dateTo;
            public string term;
            public List<SongStatus> songStatuses;
        }

        public class GuidName
        {
            public string Guid;
            public string Name;

            public GuidName() { }
            public GuidName(Guid id, string name)
            {
                Guid = id.ToString();
                Name = name;
            }
        }
        public class AjaxMgrSample
        {
            public string Id;
            public string Created;
            public decimal Duration;
            public string Title;
            public string Product;
            public string Brand;
            public string Company;
            public string Category;
            public string Campaign;
            public string MessageType;
            public int? Queued;
            public int Transcribed;
            public byte Priority;
            public int CorrectionPendingCount;
            public int CorrectedCount;
            public int MasterCount;
            public int TrashedCount;

            public AjaxMgrSample() { }
            public AjaxMgrSample(Model.Sample sample)
            {
                Id = sample.Id.ToString();
                Created = sample.Created.HasValue ? sample.Created.Value.ToString("o") : null;
                Duration = decimal.ToInt32(sample.Duration * 1000);
                Title = sample.Title;
                Product = sample.Product;
                Brand = sample.Brand;
                Company = sample.Company;
                Category = sample.Category;
                Campaign = sample.Campaign;
                MessageType = sample.MessageType;
                Queued = sample.Queued;
                Transcribed = sample.Transcribed;
                Priority = sample.Priority;
                CorrectionPendingCount = sample.CorrectionPendingCount;
                CorrectedCount = sample.CorrectedCount;
                MasterCount = sample.MasterCount;
                TrashedCount = sample.TrashedCount;
            }
        }
        public class AjaxTagAttribute : GuidName
        {
            public string Type;
            public AjaxTagAttribute() { }
            internal protected AjaxTagAttribute(Model.TaggerBrand brand)
            {
                Guid = brand.Id.ToString();
                Name = brand.Name;
                Type = Model.TaggerAttribute.AttributeType.Brand.ToString();
            }
            internal protected AjaxTagAttribute(Model.TaggerCompany company)
            {
                Guid = company.Id.ToString();
                Name = company.Name;
                Type = Model.TaggerAttribute.AttributeType.Company.ToString();
            }
            internal protected AjaxTagAttribute(Model.TaggerAttribute attribute)
            {
                Guid = attribute.Id.ToString();
                Name = attribute.Name;
                Type = attribute.Type.ToString();
            }
        }
        public class AjaxChannel
        {
            public Guid Id;
            public string Name;
            public string City;
            public string Country;
            public string MediaType;
            public int CityCount;

            public AjaxChannel(Model.Channel channel)
            {
                Id = channel.Id;
                Name = channel.Name;
                City = channel.City;
                Country = channel.Country;
                MediaType = channel.MediaType;
            }
        }
        public class AjaxTranscript
        {
            public string SongId;
            public string PksId;
            public string FilePath;
            public bool Training;
            public int? ReviewOf;
            /// <summary>
            /// Sample duration in milliseconds
            /// </summary>
            public int Duration;

            public AjaxTranscriptPart[] Parts;
			public string SongTitle;
			public string Brand;
			public string Domain;

            public AjaxTranscript() { }
            public AjaxTranscript(Model.Transcript transcript)
            {
                SongId = transcript.SongId.ToString();
                PksId = transcript.PksId;
                FilePath = Model.TaggerSong.GetMp3Url(transcript.PksId);
                Training = transcript.Training;
                ReviewOf = transcript.ReviewOf;
                Duration = decimal.ToInt32(transcript.Duration * 1000);
                Parts = transcript.Parts.Select(p => new AjaxTranscriptPart(p)).ToArray();
				SongTitle = transcript.SongTitle;
				Brand = transcript.Brand;
				Domain = transcript.Domain;
            }
            public Model.Transcript ToModel(string userId)
            {
                var t = new Model.Transcript
                {
                    UserId = userId,
                    SongId = Guid.Parse(SongId),
                    PksId = PksId,
                    Training = Training,
                    ReviewOf = ReviewOf,
                    Duration = (decimal)Duration / 1000,
                    Parts = Parts.Select(part => part.ToModel()).ToList()
                };

                if (t.Parts.Any())
                {
                    t.EditStart = t.Parts.Min(p => p.EditStart);
                    t.EditEnd = t.Parts.Max(p => p.EditEnd);
                }

                return t;
            }
        }
        public class AjaxTranscriptPart
        {
            public int TimeStart;
            public int TimeEnd;

            public string Text;

            public string EditStart;
            public string EditEnd;

            public AjaxTranscriptPart() { }
            public AjaxTranscriptPart(Model.Transcript.Part part)
            {
                TimeStart = decimal.ToInt32(part.TimeStart * 1000);
                TimeEnd = decimal.ToInt32(part.TimeEnd * 1000);

                Text = part.Text;
            }
            public Model.Transcript.Part ToModel()
            {
                return new Model.Transcript.Part
                {
                    TimeStart = ((decimal)TimeStart) / 1000,
                    TimeEnd = ((decimal)TimeEnd / 1000),
                    EditStart = DateTime.SpecifyKind(DateTime.Parse(EditStart), DateTimeKind.Utc),
                    EditEnd = DateTime.SpecifyKind(DateTime.Parse(EditEnd), DateTimeKind.Utc),
                    Text = Text != null ? Text.Trim() : string.Empty
                };
            }
        }
        public class AjaxTranscriptFilter
        {
            public string startDate;
            public string endDate;
            public string text;
            public object status;
            public byte priority;
            public object product;
            public object category;
            public object company;
            public object brand;
        }
        public static class DateUtility
        {
            public static DateTime? GetDate(string sDate)
            {
                DateTime startDate;
                return DateTime.TryParse(sDate, out startDate) ? startDate : (DateTime?)null;
            }

			public static string ToISODateTime(DateTime? date)
			{
				if (date == null)
					return null;
				return date.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
			}
        }
        public class AjaxTranscriptStatistic
        {
            public int Id;
            public string UserId;
            public string Email;
            public string Name;
            public string Day;
            public int SongCount;
            /// <summary>
            /// Edit time in seconds
            /// </summary>
            public decimal EditTime;
            /// <summary>
            /// Sample duration in seconds
            /// </summary>
            public decimal SongDuration;
            public string Text;

            public decimal? Performance;

            public AjaxTranscriptStatistic(Model.TranscriptStatistic stat)
            {
                Id = stat.Id;
                UserId = stat.UserId;
                Email = stat.Email;
                Name = stat.Name;
                Day = stat.Day == null ? null : stat.Day.Value.ToString("yyyy-MM-dd HH:mm:ss");
                SongCount = stat.SongCount;
                EditTime = stat.EditTime;
                SongDuration = stat.SongDuration;
                Text = stat.Text;
                Performance = stat.Performance;
            }
        }
        public class AjaxTranscriptEarning
        {
            public string Start;
            public string End;
            public decimal EditTime;
            public decimal SongDuration;
            public decimal Earning;

            public AjaxTranscriptEarning(mhr.Period period, IEnumerable<Model.TranscriptStatistic> stats)
            {
                stats = stats
                     .Where(s => s.Day >= period.CurrentStart && s.Day < period.CurrentEnd);

                Start = period.CurrentStart.ToString("yyyy-MM-dd hh:mm:ss");
                End = period.CurrentEnd.ToString("yyyy-MM-dd hh:mm:ss");
                EditTime = Math.Round(stats.Select(s => s.EditTime).DefaultIfEmpty().Sum());
                SongDuration = Math.Round(stats.Select(s => s.SongDuration).DefaultIfEmpty().Sum());
                Earning = Model.Transcript.Configuration.Get().EarningPerSecond * SongDuration;
            }
        }
        public class AjaxWordCut
        {
            // Time positions and durations in miliseconds
            public int Id;
            public int TranscriptId;
            public string SongId;
            public string FilePath;
            public int PartStart;
            public int PartEnd;
            public string PartText;
            public int Duration;
            public string Word;
            public int WordStart;
            public int WordDuration;

            public AjaxWordCut() { }
            public AjaxWordCut(Model.WordCut wc)
            {
                Id = wc.Id;
                TranscriptId = wc.TranscriptId;
                SongId = wc.SongId.ToString();
                FilePath = Model.TaggerSong.GetMp3Url(wc.PksId);
                PartStart = decimal.ToInt32(wc.PartStart * 1000);
                PartEnd = decimal.ToInt32(wc.PartEnd * 1000);
                PartText = wc.PartText;
                Duration = decimal.ToInt32(wc.Duration * 1000);
                Word = wc.Word;
                WordStart = decimal.ToInt32(wc.WordStart * 1000);
                WordDuration = decimal.ToInt32(wc.WordDuration * 1000);
            }
        }
        public class AjaxSampleMatch
        {
            public string SourcePksid;
            public string SourceMp3Path;
            public string CustomerName;
            public int Duration;
            public string FirstWords;
            public string Harvester;
            public string CreateDate;
            public string ExportDate;
            public string Station;
            public AjaxMatch[] Matches;
            public object[] GroupedMatches;
            public int QueueLength;

            public AjaxSampleMatch() { }
            public AjaxSampleMatch(Model.SampleMatch ms, int queueLength)
            {
                SourcePksid = ms.PksId;
                SourceMp3Path = Model.TaggerSong.GetMp3Url(ms.PksId, ms.CustomerName);
                CustomerName = ms.CustomerName;
                Duration = decimal.ToInt32(ms.Duration * 1000);
                FirstWords = ms.FirstWords;
                CreateDate = ms.CreateDate.HasValue ? ms.CreateDate.Value.ToString("s") : null;
                QueueLength = queueLength;

                var clipForInbox = Model.SampleMatch.DownloadClipForInbox(ms.CustomerName, ms.PksId);
                if (clipForInbox != null)
                {
                    Harvester = clipForInbox.Tagger;
                    CreateDate = string.IsNullOrWhiteSpace(clipForInbox.ClipDateTime) ||
                         clipForInbox.ClipDateTime.StartsWith("000") ? // Sometimes contains "0001-01-01T00:00:00"
                         null : clipForInbox.ClipDateTime;
                    ExportDate = clipForInbox.ExportDateTime;
                    Station = clipForInbox.StationName;
                }

                Matches = ms.Matches.Select(m => new AjaxMatch(m)).ToArray();
                GroupedMatches = ms.Matches.GroupBy(m => m.TargetSample, m => new
                {
                    Match = m,
                    ClipForInbox = Model.SampleMatch.DownloadClipForInbox(m.CustomerName, m.TargetSample)
                }).Select(g => new
                {
                    TargetSample = g.Key,
                    TargetMp3Path = Model.TaggerSong.GetMp3Url(g.Key),
                    Duration = decimal.ToInt32(g.First().Match.Duration * 1000),
                    Difference = g.First().Match.Difference,
                    FirstWords = g.First().Match.FirstWords,
                    Harvester = g.First().ClipForInbox != null ? g.First().ClipForInbox.Tagger : null,
                    Station = g.First().ClipForInbox != null ? g.First().ClipForInbox.StationName : null,
                    CreateDate = g.First().Match.CreateDate.HasValue ? g.First().Match.CreateDate.Value.ToString("s") : null,
                    OffsetGroups = AjaxChunk.GroupByOffset(g.Select(m => new AjaxChunk(m.Match)))
                }).ToArray();
            }
        }
        public class AjaxMatch
        {
            public string TargetSample;
            public string TargetMp3Path;
            public int SourceStart;
            public int SourceEnd;
            public int TargetStart;
            public int TargetEnd;
            public int Duration;
            public decimal Difference;
            public string FirstWords;
            public string Harvester;
            public string CreateDate;
            public string Station;

            public AjaxMatch(Model.SampleMatch.Match m)
            {
                TargetSample = m.TargetSample;
                TargetMp3Path = Model.TaggerSong.GetMp3Url(m.TargetSample);
                SourceStart = decimal.ToInt32(m.SourceStart * 1000);
                SourceEnd = decimal.ToInt32(m.SourceEnd * 1000);
                TargetStart = decimal.ToInt32(m.TargetStart * 1000);
                TargetEnd = decimal.ToInt32(m.TargetEnd * 1000);
                Duration = decimal.ToInt32(m.Duration * 1000);
                Difference = m.Difference;
                CreateDate = m.MatchDate.ToString("s");
                FirstWords = m.FirstWords;

                var clipForInbox = Model.SampleMatch.DownloadClipForInbox(m.CustomerName, m.TargetSample);
                if (clipForInbox != null)
                {
                    Harvester = clipForInbox.Tagger;
                    Station = clipForInbox.StationName;
                }
            }
        }
        public class AjaxChunk
        {
            public int SourceStart;
            public int SourceEnd;
            public int TargetStart;
            public int TargetEnd;
            public int Offset;

            public AjaxChunk(decimal sourceStart, decimal sourceEnd, decimal targetStart, decimal targetEnd, decimal offset)
            {
                SourceStart = _InMilisec(sourceStart);
                SourceEnd = _InMilisec(sourceEnd);
                TargetStart = _InMilisec(targetStart);
                TargetEnd = _InMilisec(targetEnd);
                Offset = _InMilisec(offset);
            }

            public AjaxChunk(Model.SampleMatch.Match match)
                : this(
                     match.SourceStart,
                     match.SourceEnd,
                     match.TargetStart,
                     match.TargetEnd,
                     match.SourceStart - match.TargetStart)
            { }

            public static AjaxOffsetGroup[] GroupByOffset(IEnumerable<AjaxChunk> chunks)
            {
                var groups = new List<AjaxOffsetGroup>();
                foreach (var chunk in chunks)
                {
                    var group = groups.FirstOrDefault(g => g.Belongs(chunk));
                    if (group == null)
                    {
                        group = new AjaxOffsetGroup();
                        groups.Add(group);
                    }
                    group.Add(chunk);
                }
                return groups.ToArray();
            }

            private static int _InMilisec(decimal t)
            {
                return decimal.ToInt32(t * 1000);
            }

            public bool Overlaps(AjaxChunk other)
            {
                int tolerance = 200; // ms
                return !(
                     this.SourceEnd < other.SourceStart + tolerance ||
                     other.SourceEnd < this.SourceStart + tolerance);
            }
        }
        public class AjaxOffsetGroup
        {
            public int Offset;
            /// <summary>
            /// True when chunk offsets span over more than 200ms
            /// </summary>
            public bool LooseOffsets;
            public List<AjaxChunk> Chunks = new List<AjaxChunk>();
            public bool HasOverlaps;

            public bool Belongs(AjaxChunk chunk)
            {
                if (!Chunks.Any())
                    return true;

                int maxOffset = Math.Max(Chunks.Max(c => c.Offset), chunk.Offset);
                int minOffset = Math.Min(Chunks.Min(c => c.Offset), chunk.Offset);

                return maxOffset - minOffset <= 1000;
            }
            private int OffsetSpan()
            {
                if (!Chunks.Any())
                    return 0;

                int maxOffset = Chunks.Max(c => c.Offset);
                int minOffset = Chunks.Min(c => c.Offset);

                return maxOffset - minOffset;
            }
            public void Add(AjaxChunk chunk)
            {
                HasOverlaps = HasOverlaps || Chunks.Any(c => c.Overlaps(chunk));
                Chunks.Add(chunk);
                Offset = (int)Chunks.Average(c => c.Offset);
                int looseThreshold = 200; // ms
                LooseOffsets = OffsetSpan() > looseThreshold;
            }
        }

		public class AjaxMatches
		{
			public string id { get; set; }
			public string song_id { get; set; }
			public string channel_id { get; set; }
			public string match_occurred { get; set; }
			public double? match_start { get; set; }
			public double? match_end { get; set; }
			public double? min_ber { get; set; }
			public string date_scanned { get; set; }
			public double? earns { get; set; }
			public string earns_problem { get; set; }
			public string create_stamp { get; set; }
			public AjaxSong Song { get; set; }
			public double? duration
			{
				get
				{
					return match_end - match_start;
				}
			}

			public static AjaxMatches FromMatch(Match m)
			{
				return new AjaxMatches
				{
					id = m.id,
					song_id = m.song_id,
					channel_id = m.channel_id,
					match_occurred = DateUtility.ToISODateTime(m.match_occurred),
					match_start = m.match_start,
					match_end = m.match_end,
					min_ber = m.min_ber,
					date_scanned = DateUtility.ToISODateTime(m.date_scanned),
					earns = m.earns,
					earns_problem = m.earns_problem,
					create_stamp = DateUtility.ToISODateTime(m.create_stamp),
					Song = new AjaxSong
					{
						Created = DateUtility.ToISODateTime(m.Song.Created),
						Duration = m.Song.Duration,
						Title = m.Song.Title
					}
				};
			}
		}

		public class AjaxSong
		{
			public string Title { get; set; }
			public decimal Duration { get; set; }
			public string Created { get; set; }
		}
		#endregion

		#region Clients, Contacts

		[WebMethod]
		public object GetClients()
		{
			try { return _GetUser() != null ? Client.GetAll() : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetClient(int id)
		{
			try { return _GetUser() != null ? Client.Get(id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object CreateClient(Client c)
		{
			try
			{
				if (_GetUser() != null)
				{
					Client.Create(c);
					return c;
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object UpdateClient(Client c)
		{
			try
			{
				if (_GetUser() != null)
				{
					Client.Update(c);
					return c;
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void DeleteClient(int id)
		{
			try
			{
				if (_GetUser() != null)
				{
					Client.Delete(id);					
				}
				
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void DeleteClients(string ids)
		{
			try
			{
				var client_ids = ids.Split(',').Select(int.Parse).ToList();
				if (_GetUser() != null && client_ids.Count > 0)
				{
					Client.DeleteMultiple(client_ids);
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetContactsForClient(int client_id)
		{
			try { return _GetUser() != null ? Contact.GetForClient(client_id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object SearchContacts(string text)
		{
			try { return _GetUser() != null ? Contact.SearchContacts(text).Select(c=> new { id = c.contact_id, c.email, c.name}) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetContact(int id)
		{
			try { return _GetUser() != null ? Contact.Get(id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object CreateContact(Contact c)
		{
			try
			{
				if (_GetUser() != null)
				{
					Contact.Create(c);
					return c;
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public bool CheckContactUser(Guid user_id, int? contact_id = 0)
		{
			return contact_id == 0 ?
				Database.Exists("SELECT user_id FROM contacts WHERE user_id = @user_id", "@user_id", user_id) :
				Database.Exists("SELECT user_id FROM contacts WHERE user_id = @user_id AND contacts.contact_id <> @id", "@user_id", user_id, "@id", contact_id);
				

		}

		[WebMethod]
		public object UpdateContact(Contact c)
		{
			try
			{
				if (_GetUser() != null)
				{
					Contact.Update(c);
					return c;
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void DeleteContact(int id)
		{
			try
			{
				if (_GetUser() != null)
				{
					Contact.Delete(id);
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void DeleteContacts(string ids)
		{
			try
			{
				var contact_ids = ids.Split(',').Select(int.Parse).ToList();
				if (_GetUser() != null && contact_ids.Count > 0)
				{
					Contact.DeleteMultiple(contact_ids);
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetAllUsers()
		{
			try
			{
				if (_GetUser() != null)
				{
					return TaggerUser.GetAll().OrderBy(u=>u.Email).Select(u => new
					{
						u.Id,
						u.Email,
						u.Name
					});
				}
				return null;

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetAvailableUsers()
		{
			try
			{
				if (_GetUser() != null)
				{
					return TaggerUser.GetUsersAvailableForContacts().OrderBy(u => u.Email).Select(u => new
					{
						u.Id,
						u.Email,
						u.Name,
						u.hasContact,
						u.slug
					});
				}
				return null;

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetFeedsForContact(int contact_id)
		{
			try { return _GetUser() != null ? Feeds.GetFeedFiltersForContact(contact_id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void AddFeedsToContact(int contact_id, string feedIds)
		{
			try
			{
				if (_GetUser() != null)
				{
					Contact.AddFeeds(contact_id, feedIds.Split(',').Select(int.Parse).ToList());
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void RemoveFeedsFromContact(int contact_id, string feedIds)
		{
			try
			{
				if (_GetUser() != null)
				{
					Contact.RemoveFeeds(contact_id, feedIds.Split(',').Select(int.Parse).ToList());
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetFeedsForClient(int client_id)
		{
			try { return _GetUser() != null ? Feeds.GetFeedFiltersForClient(client_id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void AddFeedsToClient(int client_id, string feedIds)
		{
			try
			{
				if (_GetUser() != null)
				{
					Client.AddFeeds(client_id, feedIds.Split(',').Select(int.Parse).ToList());
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void RemoveFeedsFromClient(int client_id, string feedIds)
		{
			try
			{
				if (_GetUser() != null)
				{
					Client.RemoveFeeds(client_id, feedIds.Split(',').Select(int.Parse).ToList());
				}

			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public bool IsUserGrantedClientFeeds()
		{
			var user = _GetUser();
			if (user != null)
				return user.IsGrantedClientFeeds();
			return false;
		}

		[WebMethod]
		public object GetSearchPhrasesForClient(int client_id)
		{
			try { return _GetUser() != null ? Client.GetSearchPhrasesForClient(client_id).OrderBy(c=>c.id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}


		[WebMethod]
		public object GetSearchPhrasesForContact(int contact_id)
		{
			try { return _GetUser() != null ? Contact.GetSearchPhrasesForContact(contact_id) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}


		[WebMethod]
		public object AddSearchPhraseToClient(ClientSearchTerm cst)
		{
			try {

				if (_GetUser() != null)
				{
					UpdateSearchTermCache(cst.search_term);
					return Client.AddSearchPhraseToClient(cst);
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public void RemoveSearchPhraseFromClient(int id)
		{
			try {
				if (_GetUser() != null)
					Client.RemoveSearchPhraseFromClient(id);
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}
		[WebMethod]
		public object AddSearchPhraseToContact(ContactSearchTerm cst)
		{
			try
			{

				if (_GetUser() != null)
				{
					UpdateSearchTermCache(cst.search_term);
					return (long?)Contact.AddSearchPhraseToContact(cst);
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}
		[WebMethod]
		public void RemoveSearchPhraseFromContact(int id)
		{
			try { if(_GetUser() != null)
					Contact.RemoveSearchPhraseFromContact(id);
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public object GetCommonSearchTerms()
		{
			return Client.GetCommonTerms();
		}

		[WebMethod]
		public object GetClientContactSearchTerms(string term)
		{
			var searchTerms = HttpContext.Current.Cache["clientcontactSearchTerms"] as List<ClientSearchTerm>;
			if(searchTerms == null)
			{
				searchTerms = Client.GetCommonTerms().Select(t => new ClientSearchTerm { common_id = t.id, search_term = t.name }).ToList();
				searchTerms.AddRange(Client.GetUniquePhrasesForClients().Select(p=> new ClientSearchTerm { common_id = null, search_term = p }));
				searchTerms.AddRange(Contact.GetUniquePhrasesForContacts().Select(p=> new ClientSearchTerm { common_id = null, search_term = p }));
				searchTerms = searchTerms.Distinct().ToList();
				HttpContext.Current.Cache["clientcontactSearchTerms"] = searchTerms;
			}
			var culture = CultureInfo.CurrentCulture;
			return searchTerms.Where(t => culture.CompareInfo.IndexOf(t.search_term, term, CompareOptions.IgnoreCase) >= 0);
		}

		[WebMethod]
		public object UpdateClientSearchTerm(ClientSearchTerm term)
		{
			return Client.UpdateSearchTerm(term);
		}

		private void UpdateSearchTermCache(string term)
		{
			if(!string.IsNullOrEmpty(term))
			{
				var cache = HttpContext.Current.Cache["clientcontactSearchTerms"] as List<ClientSearchTerm>;
				if (cache != null)
				{
					var culture = CultureInfo.CurrentCulture;					 
					if (cache.Count(t => t.search_term == term) == 0)
						cache.Add(new ClientSearchTerm { search_term = term });
				}
			}
			
		}



		#endregion

		[WebMethod]
		public object GetUsersWithDomains()
		{
			return new
			{
				usersWithDomains = TaggerUser.GetUsersWithDomains().OrderBy(u=>u.Email),
				domains = Domain.GetAll()
			};
		}

		[WebMethod]
		public void UpdateUserDomain(string userId, int domainId, bool value)
		{
			TaggerUser.UpdateUserDomain(userId, domainId, value);
		}

		#region Webplayer
		[WebMethod]
		public List<ClipValues> SearchClipValues(int? list_id, string text)
		{
			return ClipValues.Search(list_id, text);
		}

		[WebMethod]
		public List<ClipTag> SearchClipTags(string text)
		{
			return ClipTag.Search(text);
		}

		[WebMethod]
		public ClipDto CreateClip(Clip clip)
		{
			var user = _GetUser();
			
			clip.segment_youtube_id = null;
			
			clip.user_name = user.Name;
			clip.user_id = user.Id;
			clip.pksid = Guid.NewGuid().ToString();
			
			clip.ave = clip.ave ?? 0;
			clip.ave_per_30_sec = clip.ave_per_30_sec ?? 0;
			clip.tams = clip.tams ?? 0;
			clip.id = Clip.Create(clip);
			return ClipDto.FromClip(clip);
		}

		[WebMethod]
		public ClipDto UpdateClip(Clip clip)
		{
			var user = _GetUser();
			if(string.IsNullOrEmpty(clip.user_id))
			{
				clip.user_id = user.Id;
			}
			Clip.Update(clip);
			return ClipDto.FromClip(clip);
		}

		[WebMethod]
		public Clip UpdateClip2(Clip clip)
		{
			return null;
		}

		[WebMethod]
		public void DeleteClip(int id)
		{
			Clip.Delete(id);
		}

		[WebMethod]
		public List<Channel> GetSubscribedChannels()
		{
			var user = _GetUser();
			if(user != null)
			{
				return Channel.GetSubscribed(user.Id);
			}
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="segmentStart"></param>
		/// <param name="type"> 1= news, 2 = ratings</param>
		/// <returns></returns>
		[WebMethod]		
		public decimal? GetPricePerSecond(Guid channelId, DateTime segmentStart, int type)
		{
			var productGuid = Settings.Get("webplayer", type == 1 ? "aveproductid" : "ratingsproductid");
			if(productGuid != null)
			{
				List<PriceDefinition> definitions = PriceDefinition.Get(channelId, Guid.Parse(productGuid));
				var foundPd = definitions.Find(pp => pp.Dow == PriceDefinition.ByteDOW(segmentStart.DayOfWeek) &&
					pp.Hour == segmentStart.Hour);
				if (foundPd != null)
					return foundPd.Pps;
			}
			return null;
		}

		[WebMethod]
		public List<ClipDto> GetSegments(Guid channelId, DateTime? clipStart, DateTime? clipEnd)
		{
			var user = _GetUser();
			if(user != null)
			{
				return Clip.GetClipsByCriteria(channelId, user.Id, clipStart, clipEnd).Select(c=>ClipDto.FromClip(c)).ToList();
			}
			return null;			
		}

		[WebMethod]
		public List<ClipTag> GetClipTags()
		{
			return ClipTag.GetAll();
		}
				

		#endregion

		#region Matches api
		[WebMethod]
		public List<AjaxMatches> GetMatchesByCriteria(string key, string channelId, DateTime from, DateTime to)
		{
			if(key == ConfigurationManager.AppSettings["matchAnalyzerKey"])
				return Match.GetByCriteria(channelId, from, to).OrderBy(m=>m.match_occurred)
					.Select(AjaxMatches.FromMatch)
					.ToList();
			return null;
		}
		#endregion

		#region Price designer 2
		[WebMethod]
		public object GetChannelWithPriceDefinitions2(Guid channelId)
		{
			try { return _GetUser() != null ? ChannelWithPriceDefinitionsV2.Get(channelId, _GetUserId()) : null; }
			catch (Exception exc) { Log.Error(exc); throw; }
		}

		[WebMethod]
		public List<PriceDefinition2> SaveChannelPriceDefinitions2(List<PriceDefinition2> priceDefs, IList<int> deletedIds)
		{
			try
			{
				var user = _GetUser();
				if (user != null)
				{
					if (deletedIds.Count > 0)
						PriceDefinition2.DeleteByIds(deletedIds);
					return PriceDefinition2.AddOrUpdate(priceDefs);
				}
				return null;
			}
			catch (Exception exc) { Log.Error(exc); throw; }
		}


		#endregion

	}
}

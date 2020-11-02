using AdK.Tagger.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Services
{
    public enum PkSampeStatusType
    {
        OK = 1,
        ERROR = 2,
        QUEUED = 3
    }

    public class PkConnector
    {
        private readonly string ApiKey;

        public PkConnector(TaggerUser user) {
            string username = user.Id.Replace("-", "");
            string password = user.Pkspwd;

            //Create PK user if it does not exist
            if (String.IsNullOrWhiteSpace(password)) {
                password = GeneratePassword();
                App.Log.Info("Create PK user for id: {0} and pswd: {1}", user.Id, password);
                CreatePkUser(username, password);
                user.UpdatePkspwd(password);
            }

            //Authenticate user to PK
            ApiKey = Login(username, password);
        }

        #region Public methods
        public SampleStatus GetSampleStatus(string sampleId)
        {
            using (var service = new SpotUploadService.ServiceProviderSoapClient())
            {
                string pkStatus = service.GetSampleStatus(ApiKey, sampleId);

                var status = new SampleStatus {
                    PkStatus = pkStatus
                };

                if (pkStatus.StartsWith("QUEUED"))
                {
                    status.Status = PkSampeStatusType.QUEUED;
                }

                else if (pkStatus.StartsWith("OK"))
                {
                    status.Status = PkSampeStatusType.OK;

                    int iDuration = int.Parse(pkStatus.Substring("OK".Length).Trim());
                    decimal dDuration = (decimal)iDuration / 1000;

                    status.Duration = dDuration;
                }

                else if (pkStatus.StartsWith("ERROR"))
                {
                    status.Status = PkSampeStatusType.ERROR;
                }

                return status;
            }
        }

        public string AddSampleFromURL(string sampleUrl)
        {
            using (var service = new SpotUploadService.ServiceProviderSoapClient()) {
                return service.AddSampleFromURL(ApiKey, sampleUrl);
            }
        }
        #endregion


        #region Private methods
        private string GeneratePassword()
        {
            return Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 15);
        }

        private void CreatePkUser(string userId, string password)
        {
            using (var service = new SpotUploadService.ServiceProviderSoapClient()) {
                string pkResponse = service.CreateUser(userId, password);

                string callerInfo = String.Format("username: {0} password: {1}", userId, password);
                if (pkResponse == "NO ACCESS") {
                    throw new UnauthorizedAccessException(WrapExceptionMessage("Failed creating new user PK returned no-access", userId, password));
                }

                if (pkResponse == "ERROR USER EXISTS") {
                    throw new Exception(WrapExceptionMessage("User already exist on PK", userId, password));
                }
            }
        }

        private string Login(string userId, string password)
        {
            using (var service = new SpotUploadService.ServiceProviderSoapClient()) {

                string pkResponse = service.Login(userId, password);

                if (pkResponse == "invalid" || pkResponse == "confirmation_pending") // Account doesn't exist
                {
                    throw new UnauthorizedAccessException(WrapExceptionMessage("User login to PK is invalid.", userId, password));
                }

                return pkResponse;
            }
        }

        private string WrapExceptionMessage(string msg, string userId, string password)
        {
            return String.Format("{0} >> userId: {1} password: {2}", msg, userId, password);
        }
        #endregion
    }

    public class SampleStatus
    {
        public string PkStatus { get; set; }
        public PkSampeStatusType Status { get; set; }
        public string StatusString
        {
            get
            {
                return Enum.GetName(typeof(PkSampeStatusType), Status);
            }
        }
        public string Error
        {
            get
            {
                if (Status == PkSampeStatusType.ERROR)
                {
                    return PkStatus.Substring("ERROR".Length).Trim();
                }

                return null;
            }
        }

        public int? QueuePosition
        {
            get
            {
                if (Status == PkSampeStatusType.QUEUED)
                {
                    int position;
                    if (Int32.TryParse(PkStatus.Substring("QUEUED".Length).Trim(), out position))
                    {
                        return position;
                    }
                }

                return null;
            }
        }
        public decimal Duration { get; set; }
    }
}

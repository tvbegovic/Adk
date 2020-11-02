using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.AppSettings
{
	public class AdKSettings : IApplicationSettings
	{
		private const string MODULE = "AdK";

		public string RegistrationMailSubject { get; set; }
		public string RegistrationMailBody { get; set; }

		public static IApplicationSettings Get()
		{
			return new AdKSettings {
				RegistrationMailSubject = Settings.Get( MODULE, "RegistrationMailSubject", "Successful registration" ),
				RegistrationMailBody = Settings.Get( MODULE, "RegistrationMailBody",
					@"<p></p>
					  <p>To finish the registration process, please confirm your account and e-mail address by clicking the following link in the next 24 hours:</p>
					  <p>[verificationLink]<p/>
					  <p>Best regards, </br> AdK user support</p>" )
			};
		}

		public static void Save( IApplicationSettings settings )
		{
			Settings.Set( MODULE, "RegistrationMailSubject", settings.RegistrationMailSubject );
			Settings.Set( MODULE, "RegistrationMailBody", settings.RegistrationMailBody );
		}
	}
}

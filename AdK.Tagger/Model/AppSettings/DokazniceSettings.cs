using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.AppSettings
{
	public class DokazniceSettings : IApplicationSettings
	{
		private const string MODULE = "Dokaznice";

		public string RegistrationMailSubject { get; set; }
		public string RegistrationMailBody { get; set; }

		public static IApplicationSettings Get()
		{
			return new DokazniceSettings {
				RegistrationMailSubject = Settings.Get( MODULE, "RegistrationMailSubject", "Uspješno ste se registrirali" ),
				RegistrationMailBody = Settings.Get( MODULE, "RegistrationMailBody",
					@"<p>Poštovani,</p>
					  <p>Da bi uspješno zaključili registraciju molimo Vas potvrdite Vaš korisnički račun i Vašu e-mail adresu tako da posjetite donji link unutar slijedeća 24 sata:</p>
					  <p>[verificationLink]<p/>
					  <p>Srdačan pozdrav, </br> Dokaznice - Korisnička podrška</p>" )
			};
		}

		public static void Save( IApplicationSettings settings )
		{
			Settings.Set( MODULE, "RegistrationMailSubject", settings.RegistrationMailSubject );
			Settings.Set( MODULE, "RegistrationMailBody", settings.RegistrationMailBody );
		}
	}
}
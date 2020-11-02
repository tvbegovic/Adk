using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.AppSettings
{
	public class AuditSettings
	{
		private const string MODULE = "Audit";
		public bool SendMailOnSpotScan { get; set; }
		public string SpotScanMailSubject { get; set; }
		public string SpotScanMailBody { get; set; }
		public string SpotScanMailMockReciver { get; set; }

		public static AuditSettings Get()
		{
			return new AuditSettings
			{
				SendMailOnSpotScan = Settings.Get( MODULE, "SendMailOnSpotScan", false ),
				SpotScanMailMockReciver = Settings.Get( MODULE, "SpotScanMailMockReciver", "" ),
				SpotScanMailSubject = Settings.Get( MODULE, "SpotScanMailSubject", "Vaši spotovi su skenirani i možete raditi dokaznice" ),
				SpotScanMailBody = Settings.Get( MODULE, "SpotScanMailBody",
					@"<p>Sustav je provjerio i zabilježio puštanja i 30 dana unatrag</p>
					  <p>Ulogirajte se na [loginLink]</p>
					  <p>Prebacite [numberOfScannedSpots] skenirane spotove u Spot Library</p>
					  <p>Na Quick Auditu zadajte spotove, stanice i date-range te kliknite ""run audit""</p>" )
			};
		}

		public static void Save(AuditSettings settings)
		{
			Settings.Set( MODULE, "SendMailOnSpotScan", settings.SendMailOnSpotScan );
			Settings.Set( MODULE, "SpotScanMailSubject", settings.SpotScanMailSubject );
			Settings.Set( MODULE, "SpotScanMailBody", settings.SpotScanMailBody );
			Settings.Set( MODULE, "SpotScanMailMockReciver", settings.SpotScanMailMockReciver );
		}
	}
}
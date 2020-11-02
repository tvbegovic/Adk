using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Channels;
using System.Web;
using AdK.Tagger.Model.AppSettings;
using NLog;

namespace AdK.Tagger.Model
{
	public enum ApplicationEnum
	{
		Undefined = 0,
		AdKontrol = 1,
		Dokaznice = 2,

	}

	public static class Application
	{
		private static ApplicationEnum _identifier { get; set; }

		public static ApplicationEnum Identifier
		{
			get { return _identifier; }
		}


		static Application()
		{
			string application = ConfigurationManager.AppSettings["Application"];

			switch ( application.ToLower() ) {
				case "adkontrol":
					_identifier = ApplicationEnum.AdKontrol;
					break;
				case "dokaznice":
					_identifier = ApplicationEnum.Dokaznice;
					break;
				default:
					_identifier = ApplicationEnum.Undefined;
					break;
			}
		}


		public static bool IsDokaznice
		{
			get { return Identifier == ApplicationEnum.Dokaznice; }
		}

		public static bool IsAdK
		{
			get { return Identifier == ApplicationEnum.AdKontrol; }
		}

		public static string Name
		{
			get { return IsDokaznice ? "Dokaznice" : "AdK"; }
		}

		public static IApplicationSettings GetApplicationSettings()
		{
			return IsDokaznice ? DokazniceSettings.Get() : AdKSettings.Get();
		}


	}
}

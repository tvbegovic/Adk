using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model.AppSettings
{
	public interface IApplicationSettings
	{
		string RegistrationMailSubject { get; set; }
		string RegistrationMailBody { get; set; }
	}

}
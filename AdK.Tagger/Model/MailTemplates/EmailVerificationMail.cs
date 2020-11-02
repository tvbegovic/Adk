using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.MailTemplates
{
	public class EmailVerificationMail
	{
		public static void Send( TaggerUser user, string emailToken )
		{
			Uri url = HttpContext.Current.Request.Url;
			string verificationLink = string.Format( "{0}://{1}{2}/login.html?verification={3}",
				 url.Scheme,
				 url.Authority,
				 HttpRuntime.AppDomainAppVirtualPath == "/" ? "" : HttpRuntime.AppDomainAppVirtualPath,
				 emailToken );

			var settings = Application.GetApplicationSettings();

			Mailer.Send( user.Email, settings.RegistrationMailSubject, settings.RegistrationMailBody.Replace( "[verificationLink]", verificationLink ), isHtml:true );
		}
	}
}
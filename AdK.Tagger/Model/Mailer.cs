using System.Text.RegularExpressions;
using DatabaseCommon;
using System;
using System.Net;
using System.Net.Mail;
using AdK.Tagger.Services;
using System.Text;
using System.Collections.Generic;
using System.Configuration;

namespace AdK.Tagger.Model
{
    public static class Mailer
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public static bool Send(string destination, string subject, string message, bool isHtml = false, IList<Attachment> attachments = null, Configuration Config = null, string bcc = null)
        {
            var mail = new MailMessage();
            if (!string.IsNullOrEmpty(destination))
            {
                foreach (string address in destination.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    address.Trim();
                    mail.To.Add(new MailAddress(address));
                }
            }

            var config = Config;
            if (config == null)
            {
                config = Configuration.Get();
#if DEBUG
				var webConfig = Configuration.GetFromConfig();
				config.CredentialPassword = webConfig.CredentialPassword;
#endif
			}
            if (!string.IsNullOrWhiteSpace(config.DefaultBccEmail))
            {
                mail.Bcc.Add(new MailAddress(config.DefaultBccEmail));
            }
			if(!string.IsNullOrEmpty(bcc))
			{
				mail.Bcc.Add(bcc);
			}

            mail.From = new MailAddress(config.DefaultFromEmail, config.DefaultFromName);
            mail.Subject = subject;
            mail.Body = message;
            mail.IsBodyHtml = isHtml;
            if (attachments != null)
            {
                foreach (var at in attachments)
                {
                    mail.Attachments.Add(at);
                }
            }

            Log.Info(String.Format("Send Mail to {0}; subject {1}", destination, subject));

            return Send(mail, config);
        }
        public static bool Send(MailMessage mail, Configuration config = null)
        {
            bool sent = true;

            if (config == null)
                config = Configuration.Get();

            var smtp = new SmtpClient(config.SmtpDomain, config.SmtpPort);
            smtp.EnableSsl = config.EnableSsl;
            //if (smtp.EnableSsl)
                smtp.Credentials = new NetworkCredential(config.CredentialLogin, config.CredentialPassword);

            try
            {
                smtp.Send(mail);
            }
            catch (Exception err)
            {
                App.Log.Error(err);
                sent = false;
            }
            finally
            {
                mail.Dispose();
            }

            return sent;
        }

        public class Configuration
        {
            public string SmtpDomain;
            public int SmtpPort;
            public bool EnableSsl;
            public string CredentialLogin;
            public string CredentialPassword;
            public string DefaultFromEmail;
            public string DefaultFromName;
            public string DefaultBccEmail;

            public const string FakePassword = "***";

            private const string _Module = "Mail";
            /// <summary>
            /// Read configuration from the database, table 'settings'.
            /// </summary>
            /// <returns></returns>
            public static Configuration Get()
            {
                return new Configuration
                {
                    SmtpDomain = Settings.Get(_Module, "Smtp.Domain"),
                    SmtpPort = Settings.Get(_Module, "Smtp.Port", 25),
                    EnableSsl = Settings.Get(_Module, "EnableSsl", false),
                    CredentialLogin = Settings.Get(_Module, "Credential.Login"),
                    CredentialPassword = Settings.Get(_Module, "Credential.Password"),
                    DefaultFromEmail = Settings.Get(_Module, String.Format("{0}.DefaultFrom.Email", Application.Name)),
                    DefaultFromName = Settings.Get(_Module, String.Format("{0}.DefaultFrom.Name", Application.Name)),
                    DefaultBccEmail = Settings.Get(_Module, "DefaultBcc.Email")
                };
            }
            /// <summary>
            /// Gets email configuration from app.config or web.config
            /// </summary>
            /// <returns></returns>
            public static Configuration GetFromConfig()
            {
                return new Configuration
                {
                    SmtpDomain = ConfigurationManager.AppSettings["Tagger.Mail.Smtp.Domain"],
                    SmtpPort = int.Parse(ConfigurationManager.AppSettings["Tagger.Mail.Smtp.Port"]),
                    EnableSsl = bool.Parse(ConfigurationManager.AppSettings["Tagger.Mail.EnableSsl"]),
                    CredentialLogin = ConfigurationManager.AppSettings["Tagger.Mail.Credential.Login"],
                    CredentialPassword = ConfigurationManager.AppSettings["Tagger.Mail.Credential.Password"],
                    DefaultFromEmail = ConfigurationManager.AppSettings["Tagger.Mail.DefaultFrom.Email"],
                    DefaultFromName = ConfigurationManager.AppSettings["Tagger.Mail.DefaultFrom.Name"],
                    DefaultBccEmail = ConfigurationManager.AppSettings["Tagger.Mail.DefaultBcc.Email"]
                };
            }

            public void Save()
            {
                Settings.Set(_Module, "Smtp.Domain", SmtpDomain);
                Settings.Set(_Module, "Smtp.Port", SmtpPort);
                Settings.Set(_Module, "EnableSsl", EnableSsl);
                Settings.Set(_Module, "Credential.Login", CredentialLogin);
                if (CredentialPassword != FakePassword)
                    Settings.Set(_Module, "Credential.Password", CredentialPassword);
                Settings.Set(_Module, String.Format("{0}.DefaultFrom.Email", Application.Name), DefaultFromEmail);
                Settings.Set(_Module, String.Format("{0}.DefaultFrom.Name", Application.Name), DefaultFromName);
                Settings.Set(_Module, "DefaultBcc.Email", DefaultBccEmail);
            }
        }

        public static string ProcessTemplate(string messageText, params object[] vars)
        {
            string retval = messageText;
            for (int n = 0; n < vars.Length; n += 2)
            {
                if (vars[n + 1] is object[])
                {
                    StringBuilder sb = new StringBuilder();
                    string startMark = string.Format("[{0}-START]", vars[n]);
                    string endMark = string.Format("[{0}-END]", vars[n]);

                    int repeaterStart = retval.IndexOf("<p>" + startMark + "</p>");
                    if (repeaterStart == -1)
                    {
                        repeaterStart = retval.IndexOf(startMark);
                        retval = retval.Replace(startMark, "");
                    }
                    else
                    {
                        retval = retval.Replace("<p>" + startMark + "</p>", "");
                    }

                    int repeaterEnd = retval.IndexOf("<p>" + endMark + "</p>");
                    if (repeaterEnd == -1)
                    {
                        repeaterEnd = retval.IndexOf(endMark);
                        retval = retval.Replace(endMark, "");
                    }
                    else
                    {
                        retval = retval.Replace("<p>" + endMark + "</p>", "");
                    }

                    if (repeaterStart == -1 && repeaterEnd == -1)
                        continue;

                    if (repeaterStart > -1 && repeaterEnd == -1)
                        Log.Warn("Email template processing: Repeater START tag without END tag found.");
                    else if (repeaterEnd > -1 && repeaterStart == -1)
                        Log.Warn("Email template processing: Repeater END tag without START tag found.");

                    string repeaterBlock = retval.Substring(repeaterStart, repeaterEnd - repeaterStart);

                    if (repeaterBlock.Contains("<table>"))
                    {
                        repeaterEnd = repeaterStart + repeaterBlock.IndexOf("</tbody>") + "</tbody>".Length;
                        repeaterStart += repeaterBlock.IndexOf("<tbody>");
                        repeaterBlock = retval.Substring(repeaterStart, repeaterEnd - repeaterStart);
                    }

                    for (int m = 0; m < ((object[])vars[n + 1]).Length; m++)
                    {
                        object[] blockVars = (object[])((object[])vars[n + 1])[m];
                        sb.Append(ProcessTemplate(repeaterBlock, blockVars));
                    }

                    string part1 = retval.Substring(0, repeaterStart);
                    string part2 = retval.Substring(repeaterEnd);
                    retval = part1 + sb.ToString() + part2;
                }
                else
                {
                    retval = retval.Replace(string.Format("[{0}]", vars[n]), (vars[n + 1] ?? string.Empty).ToString());
                }
            }
            return retval;
        }
    }
}

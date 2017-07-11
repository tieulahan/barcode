using System;
using System.Configuration;
using System.Net.Mail;
using Merit.BarCodeScanner.Models;

namespace Merit.BarCodeScanner.Helpers
{
    public class EmailHelper
    {
        public static ResultRespose SendMail(AppSettingsSection _appSettings, EmailContent emailContent)
        {
            var host = _appSettings.Settings["HostMail"]?.Value;
            var from = _appSettings.Settings["SendFrom"]?.Value;
            var to = _appSettings.Settings["SendTo"]?.Value;
            var port = _appSettings.Settings["PortMail"]?.Value;

            try
            {
                MailContent(emailContent.Subject, from, to, emailContent.Body);
                return new ResultRespose
                {
                    Status = true,                    
                };
            }
            catch (Exception exception)
            {
                return new ResultRespose
                {
                    Status = true,
                    Message = exception.Message
                };
            }
        }

        private static void MailContent(string subject, string from, string to, string body)
        {

            var mailMessage = new MailMessage
            {
                From = new MailAddress(from),
                IsBodyHtml = true,
                Subject = subject,
                Body = body
            };

            mailMessage.To.Add(to);

            var smtpClient = new SmtpClient();
            smtpClient.Send(mailMessage);
        }
    }
}

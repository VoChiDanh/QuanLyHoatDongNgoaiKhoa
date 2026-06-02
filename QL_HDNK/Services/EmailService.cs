using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace QL_HDNK.Services
{
    public class EmailService
    {
        public void Send(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                return;
            }

            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var password = ConfigurationManager.AppSettings["SmtpPassword"];
            var from = ConfigurationManager.AppSettings["SmtpFrom"] ?? user;
            var fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "QUẢN LÝ HOẠT ĐỘNG NGOẠI KHÓA";
            var enableSsl = bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"], out var ssl) && ssl;
            var port = int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var parsedPort) ? parsedPort : 587;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || password == "CHANGE_ME")
            {
                throw new InvalidOperationException("Chưa cấu hình SMTP trong Web.config.");
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(from, fromName, Encoding.UTF8);
                message.To.Add(to);
                message.Subject = subject;
                message.SubjectEncoding = Encoding.UTF8;
                message.Body = body;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = enableSsl;
                    if (!string.IsNullOrWhiteSpace(user))
                    {
                        client.Credentials = new NetworkCredential(user, password);
                    }

                    client.Send(message);
                }
            }
        }
    }
}

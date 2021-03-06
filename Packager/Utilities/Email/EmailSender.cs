using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using Packager.Models.EmailMessageModels;
using Packager.Models.SettingsModels;
using Packager.Providers;

namespace Packager.Utilities.Email
{
    public class EmailSender : IEmailSender
    {
        public EmailSender(IProgramSettings programSettings, IFileProvider fileProvider)
        {
            FileProvider = fileProvider;
            SmtpServer = programSettings.SmtpServer;
        }

        private IFileProvider FileProvider { get; set; }
        private string SmtpServer { get; set; }

        public void Send(AbstractEmailMessage message)
        {
            try
            {
                using (var client = new SmtpClient(SmtpServer){UseDefaultCredentials = true, EnableSsl = true})
                {
                    foreach (var address in message.ToAddresses)
                    {
                        using (var email = GetMessage(message, address))
                        {
                            client.Send(email);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private MailMessage GetMessage(AbstractEmailMessage message, string address)
        {
            var result = new MailMessage(message.From, address, message.Title, message.Body) {IsBodyHtml = true};

            foreach (var attachment in GetAttachments(message))
            {
                result.Attachments.Add(attachment);
            }

            return result;
        }

        private IEnumerable<Attachment> GetAttachments(AbstractEmailMessage message)
        {
            if (message.Attachments == null || !message.Attachments.Any())
            {
                return new List<Attachment>();
            }

            var result = new List<Attachment>();
            foreach (var filePath in message.Attachments)
            {
                if (!FileProvider.FileExists(filePath))
                {
                    continue;
                }
                
                var attachment = new Attachment(filePath, MediaTypeNames.Application.Octet);

                var info = FileProvider.GetFileInfo(filePath);
                attachment.ContentDisposition.CreationDate = info.CreationTimeUtc;
                attachment.ContentDisposition.ModificationDate = info.LastWriteTimeUtc;
                attachment.ContentDisposition.ReadDate = info.LastAccessTimeUtc;
                attachment.ContentDisposition.FileName = info.Name;
                attachment.ContentDisposition.Size = info.Length;
                attachment.ContentDisposition.DispositionType = DispositionTypeNames.Attachment;
                result.Add(attachment);
            }

            return result;
        }
    }
}
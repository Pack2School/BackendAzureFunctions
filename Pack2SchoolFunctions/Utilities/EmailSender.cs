using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class EmailSender
    {

        public static async Task sendEmailAsync(string userName, string sendTo, List<string> missingSubjects)
        {
            var apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var client = new SendGridClient(apiKey);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress("pack2schooliot@gmail.com"),
                Subject = "Pack2School - Scan result",
                PlainTextContent = $"Subjects scanning was performed for the student with the following ID: {userName}. The following subjects are missing: {string.Join(ProjectConsts.delimiter, missingSubjects)}."
            };

            msg.AddTo(new EmailAddress(sendTo));
            await client.SendEmailAsync(msg);
        }
    }
}
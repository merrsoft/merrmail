﻿using System.Net;
using System.Net.Mail;
using Merrsoft.MerrMail.Application.Contracts;
using Merrsoft.MerrMail.Domain.Common;
using Merrsoft.MerrMail.Domain.Models;
using Merrsoft.MerrMail.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Merrsoft.MerrMail.Infrastructure.Services;

public class SmtpReplyService(
    ILogger<SmtpReplyService> logger,
    IOptions<EmailReplyOptions> replyContentOptions,
    IOptions<EmailApiOptions> emailApiOptions) : IEmailReplyService
{
    private readonly string _host = emailApiOptions.Value.HostAddress;
    private readonly string _password = emailApiOptions.Value.HostPassword;
    private readonly string _header = replyContentOptions.Value.Header;
    private readonly string _introduction = replyContentOptions.Value.Introduction;
    private readonly string _conclusion = replyContentOptions.Value.Conclusion;
    private readonly string _closing = replyContentOptions.Value.Closing;
    private readonly string _signature = replyContentOptions.Value.Signature;

    public void ReplyThread(EmailThread emailThread, string message)
    {
        var emailBody = new EmailReplyBuilder()
            .SetHeader(_header)
            .SetIntroduction(_introduction)
            .SetMessage(message)
            .SetConclusion(_conclusion)
            .SetClosing(_closing)
            .SetSignature(_signature)
            .Build();

        const int gmailSmtpPort = 587;
        var smtpClient = new SmtpClient("smtp.gmail.com", gmailSmtpPort);
        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtpClient.EnableSsl = true;
        smtpClient.UseDefaultCredentials = false;
        smtpClient.Credentials = new NetworkCredential(_host, _password);

        using var mailMessage = new MailMessage(_host, emailThread.Sender);
        mailMessage.Subject = "Re: " + emailThread.Subject;
        mailMessage.Headers.Add("In-Reply-To", emailThread.Id);
        mailMessage.Headers.Add("References", emailThread.Id);
        mailMessage.Body = emailBody;
        mailMessage.IsBodyHtml = true;

        try
        {
            smtpClient.Send(mailMessage);
            logger.LogInformation("Replied to thread {threadId}.", emailThread.Id);
        }
        catch (Exception ex)
        {
            logger.LogError("Error replying to thread {threadId}: {message}", emailThread.Id, ex.Message);
        }
    }
}
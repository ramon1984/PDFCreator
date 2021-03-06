﻿using NSubstitute;
using NUnit.Framework;
using pdfforge.Obsidian;
using pdfforge.PDFCreator.Conversion.Actions.Actions;
using pdfforge.PDFCreator.Conversion.Jobs;
using pdfforge.PDFCreator.Conversion.Jobs.Jobs;
using pdfforge.PDFCreator.Conversion.Settings;
using pdfforge.PDFCreator.Core.Services.Translation;
using pdfforge.PDFCreator.Core.Workflow;
using pdfforge.PDFCreator.UI.Interactions;
using pdfforge.PDFCreator.UI.Interactions.Enums;
using pdfforge.PDFCreator.UI.Presentation.Assistants;
using pdfforge.PDFCreator.UI.Presentation.DesignTime.Helper;
using pdfforge.PDFCreator.UI.Presentation.UserControls.Accounts.AccountViews;
using pdfforge.PDFCreator.UI.Presentation.UserControls.Overlay.Password;
using pdfforge.PDFCreator.UnitTest.UnitTestHelper;
using System;
using SystemInterface.IO;
using Translatable;
using Arg = NSubstitute.Arg;

namespace Presentation.UnitTest.Assistant
{
    [TestFixture]
    public class SmtpTestEmailAssistantTest
    {
        private UnitTestInteractionRequest _interactionRequest;
        private IFile _file;
        private ISmtpMailAction _smtpAction;
        private SmtpAccount _smtpTestAccount;
        private Accounts _accounts;
        private IPath _path;
        private ConversionProfile _profile;
        private IMailSignatureHelper _mailSignatureHelper;
        private SmtpTranslation _translation;
        private readonly string _mailSignature = "___ " + Environment.NewLine + "Signature";
        private IInteractionInvoker _interactionInvoker;

        [SetUp]
        public void Setup()
        {
            _smtpTestAccount = new SmtpAccount();
            _smtpTestAccount.AccountId = "SmtpTestAccountId";

            _profile = new ConversionProfile();
            //Attention
            _profile.EmailSmtpSettings.AccountId = _smtpTestAccount.AccountId;
            //The AccountAssosiation is mocked below. The _smtpTestAccount is always used.

            _accounts = new Accounts();
            _accounts.SmtpAccounts.Add(_smtpTestAccount);

            _interactionRequest = new UnitTestInteractionRequest();
            _interactionInvoker = Substitute.For<IInteractionInvoker>();
            _interactionInvoker.Invoke(Arg.Do<PasswordOverlayInteraction>(i => i.Result = PasswordResult.StorePassword));

            _interactionRequest.RegisterInteractionHandler<PasswordOverlayInteraction>(interaction => interaction.Result = PasswordResult.StorePassword);

            _file = Substitute.For<IFile>();
            _path = Substitute.For<IPath>();
            _smtpAction = Substitute.For<ISmtpMailAction>();
            _smtpAction.Check(Arg.Any<ConversionProfile>(), _accounts).Returns(x => new ActionResult());
            _smtpAction.ProcessJob(Arg.Any<Job>()).Returns(x => new ActionResult());
            //_smtpAction.GetSmtpAccount(_profile, _accounts).Returns(_smtpTestAccount);

            _mailSignatureHelper = Substitute.For<IMailSignatureHelper>();
            _mailSignatureHelper.ComposeMailSignature().Returns(_mailSignature);

            _translation = new SmtpTranslation();
        }

        private SmtpTestEmailAssistant BuildAssistant()
        {
            return new SmtpTestEmailAssistant(new DesignTimeTranslationUpdater(), _interactionRequest, _file, _smtpAction, _path, _mailSignatureHelper, new ErrorCodeInterpreter(new TranslationFactory()), _interactionInvoker);
        }

        [Test]
        public void WhenSuccessful_ShowsSuccessMessage()
        {
            var assistant = BuildAssistant();

            assistant.SendTestMail(_profile, _accounts);

            _interactionRequest.AssertWasRaised<MessageInteraction>();

            var messageInteraction = _interactionRequest.AssertWasRaised<MessageInteraction>();

            Assert.AreEqual(_translation.SendTestMail, messageInteraction.Title);
            Assert.AreEqual(_translation.GetTestMailSentFormattedTranslation(string.Empty), messageInteraction.Text);
            Assert.AreEqual(MessageIcon.Info, messageInteraction.Icon);
        }

        [Test]
        public void WhenSmtpPasswordIsSet_UsesPasswordFromAccount()
        {
            var expectedPassword = "My random password string!";
            _smtpTestAccount.Password = expectedPassword;
            var assistant = BuildAssistant();

            assistant.SendTestMail(_profile, _accounts);

            _smtpAction.Received().ProcessJob(Arg.Is<Job>(x => x.Passwords.SmtpPassword == expectedPassword));
        }

        [Test]
        public void CalledWithNotExistingAccount_InovkeCorrespondingUserInteraction()
        {
            var assistant = BuildAssistant();
            _profile.EmailSmtpSettings.AccountId = "NotExistingID";

            assistant.SendTestMail(_profile, _accounts);

            _interactionRequest.AssertWasRaised<MessageInteraction>(x => x.Text == _translation.NoAccount);
            _interactionRequest.AssertWasRaised<MessageInteraction>(x => x.Title == "PDFCreator");
            _interactionRequest.AssertWasRaised<MessageInteraction>(x => x.Buttons == MessageOptions.OK);
            _interactionRequest.AssertWasRaised<MessageInteraction>(x => x.Icon == MessageIcon.Error);
        }

        [Test]
        public void WhenCalled_WithAutoSave_DisablesAutoSave()
        {
            var expectedPassword = "My random password string!";
            _smtpTestAccount.Password = expectedPassword;
            var assistant = BuildAssistant();
            _profile.AutoSave.Enabled = true;

            assistant.SendTestMail(_profile, _accounts);

            _smtpAction.Received().ProcessJob(Arg.Is<Job>(x => x.Profile.AutoSave.Enabled == false));
        }

        [Test]
        public void SendTestMail_CallsSmtpAction()
        {
            var assistant = BuildAssistant();

            assistant.SendTestMail(_profile, _accounts);

            _smtpAction.Received().ProcessJob(Arg.Any<Job>());
        }

        [Test]
        public void WhenProfileInvalid_DisplaysErrorMessage()
        {
            var expectedError = ErrorCode.Smtp_NoPasswordSpecified;
            _smtpAction.Check(_profile, _accounts).Returns(x => new ActionResult(expectedError));
            var assistant = BuildAssistant();
            assistant.SendTestMail(_profile, _accounts);

            _interactionRequest.AssertWasRaised<MessageInteraction>();

            var messageInteraction = _interactionRequest.AssertWasRaised<MessageInteraction>();

            var errorCodeInt = (int)expectedError;
            Assert.AreEqual(_translation.SendTestMail, messageInteraction.Title);
            Assert.AreEqual(TranslationAttribute.GetValue(expectedError), messageInteraction.Text);
            Assert.AreEqual(MessageIcon.Error, messageInteraction.Icon);
        }

        [Test]
        public void WhenPasswordInteractionIsCancelled_DoesNotSendMail()
        {
            //_interactionRequest.RegisterInteractionHandler<PasswordOverlayInteraction>(interaction => interaction.Result = PasswordResult.Cancel);
            _interactionInvoker.Invoke(Arg.Do<PasswordOverlayInteraction>(i => i.Result = PasswordResult.Cancel));
            var assistant = BuildAssistant();
            assistant.SendTestMail(_profile, _accounts);

            _smtpAction.DidNotReceive().ProcessJob(Arg.Any<Job>());
        }

        [Test]
        public void WhenSendingMailFails_ShowsErrorMessage()
        {
            var expectedError = ErrorCode.Smtp_AuthenticationDenied;
            _smtpAction.ProcessJob(Arg.Any<Job>()).Returns(new ActionResult(expectedError));
            var assistant = BuildAssistant();
            assistant.SendTestMail(_profile, _accounts);

            _interactionRequest.AssertWasRaised<MessageInteraction>();

            var messageInteraction = _interactionRequest.AssertWasRaised<MessageInteraction>();

            Assert.AreEqual(_translation.SendTestMail, messageInteraction.Title);
            Assert.AreEqual(TranslationAttribute.GetValue(expectedError), messageInteraction.Text);
            Assert.AreEqual(MessageIcon.Error, messageInteraction.Icon);
        }
    }
}

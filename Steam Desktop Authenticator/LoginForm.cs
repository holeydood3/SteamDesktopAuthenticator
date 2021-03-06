﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteamAuth;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Steam_Desktop_Authenticator
{
    public partial class LoginForm : MetroFramework.Forms.MetroForm
    {

        public UserLogin mUserLogin;

        public LoginForm()
        {
            InitializeComponent();
        }

        public string FilterPhoneNumber(string phoneNumber)
        {
            return phoneNumber.Replace("-", "").Replace("(", "").Replace(")", "");
        }

        public bool PhoneNumberOkay(string phoneNumber)
        {
            if (phoneNumber == null || phoneNumber.Length == 0) return false;
            if (phoneNumber[0] != '+') return false;
            return true;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void btnSteamLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            mUserLogin = new UserLogin(username, password);
            LoginResult response = LoginResult.BadCredentials;

            while ((response = mUserLogin.DoLogin()) != LoginResult.LoginOkay)
            {
                switch (response)
                {
                    case LoginResult.NeedEmail:
                        InputForm emailForm = new InputForm("Enter the code sent to your email:");
                        emailForm.ShowDialog();
                        if (emailForm.Canceled)
                        {
                            this.Close();
                            return;
                        }

                        mUserLogin.EmailCode = emailForm.txtBox.Text;
                        break;

                    case LoginResult.NeedCaptcha:
                        System.Diagnostics.Process.Start(String.Format("{0}/public/captcha.php?gid={1}", APIEndpoints.COMMUNITY_BASE, mUserLogin.CaptchaGID));

                        InputForm captchaForm = new InputForm("Enter the captcha that opened in your browser:");
                        captchaForm.ShowDialog();
                        if (captchaForm.Canceled)
                        {
                            this.Close();
                            return;
                        }

                        mUserLogin.CaptchaText = captchaForm.txtBox.Text;
                        break;

                    case LoginResult.Need2FA:
                        MessageBox.Show("This account already has a mobile authenticator linked to it. Please remove that first.");
                        this.Close();
                        return;
                        break;
                }
            }

            //Login succeeded

            SessionData session = mUserLogin.Session;
            AuthenticatorLinker linker = new AuthenticatorLinker(session);

            AuthenticatorLinker.LinkResult linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;

            while ((linkResponse = linker.AddAuthenticator()) != AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                switch (linkResponse)
                {
                    case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
                        string phoneNumber = "";
                        while (!PhoneNumberOkay(phoneNumber))
                        {
                            InputForm phoneNumberForm = new InputForm("Enter your phone number in the following format: +{cC} phoneNumber. EG, +1 123-456-7890");
                            phoneNumberForm.txtBox.Text = "+1 ";
                            phoneNumberForm.ShowDialog();
                            if (phoneNumberForm.Canceled)
                            {
                                this.Close();
                                return;
                            }

                            phoneNumber = FilterPhoneNumber(phoneNumberForm.txtBox.Text);
                        }
                        linker.PhoneNumber = phoneNumber;
                        break;

                    case AuthenticatorLinker.LinkResult.MustRemovePhoneNumber:
                        linker.PhoneNumber = null;
                        break;

                    case AuthenticatorLinker.LinkResult.GeneralFailure:
                        this.Close();
                        return;
                        break;
                }
            }

            //Save the file immediately; losing this would be bad.
            if (!MobileAuthenticatorFileHandler.SaveMaFile(linker.LinkedAccount))
            {
                MessageBox.Show("Unable to save mobile authenticator file. The mobile authenticator has not been linked.");
                this.Close();
                return;
            }

            MessageBox.Show("The Mobile Authenticator has not yet been linked. Before finalizing the authenticator, please write down your revocation code: " + linker.LinkedAccount.RevocationCode);

            AuthenticatorLinker.FinalizeResult finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;
            while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
            {
                InputForm smsCodeForm = new InputForm("Please input the SMS code sent to your phone.");
                smsCodeForm.ShowDialog();
                if (smsCodeForm.Canceled)
                {
                    MobileAuthenticatorFileHandler.DeleteMaFile(linker.LinkedAccount);
                    this.Close();
                    return;
                }

                InputForm confirmRevocationCode = new InputForm("Please enter your revocation code to ensure you've saved it.");
                confirmRevocationCode.ShowDialog();
                if(confirmRevocationCode.txtBox.Text.ToUpper() != linker.LinkedAccount.RevocationCode)
                {
                    MessageBox.Show("Revocation code incorrect; the authenticator has not been linked.");
                    MobileAuthenticatorFileHandler.DeleteMaFile(linker.LinkedAccount);
                    this.Close();
                    return;
                }

                string smsCode = smsCodeForm.txtBox.Text;
                finalizeResponse = linker.FinalizeAddAuthenticator(smsCode);

                switch (finalizeResponse)
                {
                    case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                        continue;
                        break;

                    case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                        MessageBox.Show("Unable to generate the proper codes to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                        MobileAuthenticatorFileHandler.DeleteMaFile(linker.LinkedAccount);
                        this.Close();
                        return;
                        break;

                    case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                        MessageBox.Show("Unable to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                        MobileAuthenticatorFileHandler.DeleteMaFile(linker.LinkedAccount);
                        this.Close();
                        return;
                }
            }

            //Linked, finally. Re-save with FullyEnrolled property.
            MobileAuthenticatorFileHandler.SaveMaFile(linker.LinkedAccount);
            MessageBox.Show("Mobile authenticator successfully linked. Please write down your revocation code: " + linker.LinkedAccount.RevocationCode);
            this.Close();
        }
    }
}

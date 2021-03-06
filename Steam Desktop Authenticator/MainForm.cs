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

namespace Steam_Desktop_Authenticator
{
    public partial class MainForm : MetroFramework.Forms.MetroForm
    {
        private SteamGuardAccount mCurrentAccount = null;
        private SteamGuardAccount[] allAccounts;

        private long steamTime = 0;
        private long currentSteamChunk = 0;

        public MainForm()
        {
            InitializeComponent();
            loadAccountsList();

            pbTimeout.Maximum = 30;
            pbTimeout.Minimum = 0;
            pbTimeout.Value = 30;
        }

        private void btnSteamLogin_Click(object sender, EventArgs e)
        {
            LoginForm mLoginForm = new LoginForm();
            mLoginForm.ShowDialog();
            this.loadAccountsList();
        }

        private void listAccounts_SelectedValueChanged(object sender, EventArgs e)
        {
            // Triggered when list item is clicked
            for (int i = 0; i < allAccounts.Length; i++)
            {
                SteamGuardAccount account = allAccounts[i];
                if (account.AccountName == (string)listAccounts.Items[listAccounts.SelectedIndex])
                {
                    mCurrentAccount = account;
                    loadAccountInfo();
                }
            }
        }

        private void loadAccountsList()
        {
            mCurrentAccount = null;
            listAccounts.Items.Clear();
            listAccounts.SelectedIndex = -1;

            allAccounts = MobileAuthenticatorFileHandler.GetAllAccounts();
            if (allAccounts.Length > 0)
            {
                for (int i = 0; i < allAccounts.Length; i++)
                {
                    SteamGuardAccount account = allAccounts[i];
                    listAccounts.Items.Add(account.AccountName);
                }

                listAccounts.SelectedIndex = 0;
            }
            btnDelete.Enabled = btnTradeConfirmations.Enabled = allAccounts.Length > 0;
        }

        private void loadAccountInfo()
        {
            if (mCurrentAccount != null && steamTime != 0)
            {
                txtLoginToken.Text = mCurrentAccount.GenerateSteamGuardCodeForTime(steamTime);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            steamTime = TimeAligner.GetSteamTime();
            currentSteamChunk = steamTime / 30L;

            int secondsUntilChange = (int)(steamTime - (currentSteamChunk * 30L));

            loadAccountInfo();
            if (mCurrentAccount != null)
                pbTimeout.Value = 30 - secondsUntilChange;
        }

        private void btnTradeConfirmations_Click(object sender, EventArgs e)
        {
            if (mCurrentAccount == null) return;

            ConfirmationForm confirmations = new ConfirmationForm(mCurrentAccount);
            confirmations.ShowDialog();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (mCurrentAccount == null) return;
            string confCode = mCurrentAccount.GenerateSteamGuardCode();
            InputForm confirmationDialog = new InputForm("Removing the authenticator from " + mCurrentAccount.AccountName + ". Enter confirmation code " + confCode);
            confirmationDialog.ShowDialog();

            if (confirmationDialog.Canceled)
                return;

            string enteredCode = confirmationDialog.txtBox.Text.ToUpper();

            if (enteredCode != confCode)
            {
                MessageBox.Show("Confirmation codes do not match. Authenticator has not been unlinked.");
                return;
            }

            bool success = mCurrentAccount.DeactivateAuthenticator();
            if (success)
            {
                MessageBox.Show("Authenticator unlinked. maFile will be deleted after hitting okay. If you need to make a backup, now's the time.");
                MobileAuthenticatorFileHandler.DeleteMaFile(mCurrentAccount);
                this.loadAccountsList();
            }
            else
            {
                MessageBox.Show("Authenticator unable to be removed.");
            }
        }
    }
}

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
    public partial class ConfirmationForm : MetroFramework.Forms.MetroForm
    {
        private SteamGuardAccount mCurrentAccount;

        private Confirmation[] Confirmations;
        private Confirmation mCurrentConfirmation = null;

        public ConfirmationForm(SteamGuardAccount account)
        {
            InitializeComponent();
            this.mCurrentAccount = account;
            btnDenyConfirmation.Enabled = btnAcceptConfirmation.Enabled = false;
            loadConfirmations();
        }

        private void listAccounts_SelectedValueChanged(object sender, EventArgs e)
        {
            mCurrentConfirmation = null;

            if (listConfirmations.SelectedIndex == -1) return;

            // Triggered when list item is clicked
            for (int i = 0; i < Confirmations.Length; i++)
            {
                if (Confirmations[i].ConfirmationDescription == (string)listConfirmations.Items[listConfirmations.SelectedIndex])
                {
                    mCurrentConfirmation = Confirmations[i];
                }
            }
            btnDenyConfirmation.Enabled = btnAcceptConfirmation.Enabled = mCurrentConfirmation != null;

        }

        private void loadConfirmations()
        {
            listConfirmations.Items.Clear();
            listConfirmations.SelectedIndex = -1;

            Confirmations = mCurrentAccount.FetchConfirmations();
            if (Confirmations.Length > 0)
            {
                for (int i = 0; i < Confirmations.Length; i++)
                {
                    listConfirmations.Items.Add(Confirmations[i].ConfirmationDescription);
                }
            }
        }

        private void btnAcceptConfirmation_Click(object sender, EventArgs e)
        {
            if (mCurrentConfirmation == null) return;
            bool success = mCurrentAccount.AcceptConfirmation(mCurrentConfirmation);
            if (success)
            {
                MessageBox.Show("Confirmation successfully accepted.");
            }
            else
            {
                MessageBox.Show("Unable to accept confirmation.");
            }
            this.loadConfirmations();
        }

        private void btnDenyConfirmation_Click(object sender, EventArgs e)
        {
            if (mCurrentConfirmation == null) return;
            bool success = mCurrentAccount.DenyConfirmation(mCurrentConfirmation);
            if (success)
            {
                MessageBox.Show("Confirmation successfully denied.");
            }
            else
            {
                MessageBox.Show("Unable to deny confirmation.");
            }
            this.loadConfirmations();
        }

        private void ConfirmationForm_Load(object sender, EventArgs e)
        {

        }
    }
}

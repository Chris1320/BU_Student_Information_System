﻿#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BaliuagU_StudentInformationSheet.Handlers;
using BaliuagU_StudentInformationSheet.Models;
using BaliuagU_StudentInformationSheet.Views;
using MySql.Data.MySqlClient;

namespace BaliuagU_StudentInformationSheet.Tools
{
    public partial class ManageUsers : UserControl
    {
        private DatabaseHandler db_handler = new DatabaseHandler();
        private UserModel? current_user;
        private UserModel? active_user = null;

        public ManageUsers()
        {
            InitializeComponent();
        }

        public void SetCurrentUser(UserModel user)
        {
            this.current_user = user;
        }

        private void ManageUsers_Load(object sender, EventArgs e)
        {
            // Add columns only once when the user control loads.
            dataGridViewUsers.Columns.Add("user_id", "ID");
            dataGridViewUsers.Columns.Add("username", "Username");
            dataGridViewUsers.Columns.Add("privilege", "Privilege");
            dataGridViewUsers.Columns.Add("full_name", "Full Name");

            UpdateUsersList();
            txtName.Focus();
        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            AddUser();
            UpdateUsersList();
            txtName.Focus();
        }

        public void UpdateUsersList(string? filter_query = null)
        {
            List<UserModel> users =
                filter_query == null ? db_handler.GetAllUsers() : db_handler.SearchUsers(filter_query);

            // Add the users to the DataGridView
            dataGridViewUsers.Rows.Clear();
            foreach (var user in users)
            {
                dataGridViewUsers.Rows.Add(
                    user.user_id,
                    user.username,
                    user.privilege == Models.UserModel.Privilege.Admin ? "Super Admin" : "Admin",
                    user.full_name == null ? "N/A" : user.full_name
                );
            }
        }

        private void AddUser()
        {
            UserModel.Privilege user_privilege;
            string user_full_name = txtName.Text;

            switch (cboRole.SelectedIndex)
            {
                case 0:
                    user_privilege = UserModel.Privilege.User;
                    break;

                case 1:
                    user_privilege = UserModel.Privilege.Admin;
                    break;

                default:
                    MessageBox.Show(
                        "Please select the user privilege level.",
                        "Invalid User Type/Privilege",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation
                    );
                    return;
            }

            if (string.IsNullOrWhiteSpace(user_full_name))
            {
                MessageBox.Show(
                    "Please enter the full name of the new user.",
                    "Missing Full Name",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            if (!UserModel.ValidateUsername(txtUsername.Text))
            {
                MessageBox.Show(
                    "The username that you have entered is invalid. Only alphanumeric characters are allowed, including `-`, `_`, and `.`",
                    "Invalid Username",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            if (!UserModel.ValidatePassword(txtPassword.Text) || (txtPassword.ForeColor == Color.DimGray && txtPassword.Text == "(unchanged)"))
            {
                MessageBox.Show(
                    "The password that you have entered is invalid. The password must be at least 8 characters long.",
                    "Invalid Password",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            try
            {
                new DatabaseHandler().AddUser(
                    new UserModel(
                        user_id: -1, // this is ignored by the database handler anyway.
                        username: txtUsername.Text,
                        userpass: PasswordHandler.SHA256(txtPassword.Text),
                        privilege: user_privilege,
                        full_name: user_full_name
                    )
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An error occurred while adding the user. Please try again.\n\n" + ex.Message,
                    "Error Adding User",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
            MessageBox.Show(
                "The user has been successfully added.",
                "User Added",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void dataGridViewUsers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Load the user's profile to the form
            if (e.RowIndex < 0)
                return;

            int user_id = (int)dataGridViewUsers.Rows[e.RowIndex].Cells["user_id"].Value;
            this.active_user = db_handler.GetUser(user_id);
            if (this.active_user != null)
            {
                txtUsername.Text = active_user.username;
                //txtPassword.Text = user.userpass;
                txtPassword.Text = "(unchanged)";
                txtPassword.ForeColor = Color.DimGray;
                txtPassword.UseSystemPasswordChar = false;
                txtName.Text = active_user.full_name;
                cboRole.SelectedIndex = (int)active_user.privilege - 1;
            }
        }

        private void txtPassword_Enter(object sender, EventArgs e)
        {
            if (txtPassword.Text == "(unchanged)")
                txtPassword.Text = "";
            txtPassword.ForeColor = Color.Black;
            txtPassword.UseSystemPasswordChar = true;
        }

        private void txtPassword_Leave(object sender, EventArgs e)
        {
            if (txtPassword.Text == "")
            {
                txtPassword.Text = "(unchanged)";
                txtPassword.ForeColor = Color.DimGray;
                txtPassword.UseSystemPasswordChar = false;
            }
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            active_user = null;
            txtUsername.Text = "";
            txtPassword.Text = "";
            txtName.Text = "";
            cboRole.SelectedIndex = -1;
            cboRole.Enabled = true;
            txtUsername.Focus();
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            if (active_user == null)
            {
                MessageBox.Show(
                    "Please select a user to delete.",
                    "No User Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }
            if (db_handler.GetUsersQuantity(true) == 1 && active_user.privilege == UserModel.Privilege.Admin)
            {
                MessageBox.Show(
                    "You cannot delete the last super admin user. Please add another super admin before deleting this one.",
                    "Cannot Delete Last Super Admin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            if (active_user.user_id == (current_user == null ? 0 : current_user.user_id))
            {
                if (
                    MessageBox.Show(
                        "You will be logged out if you proceed to delete your own account. Are you sure?",
                        "Delete Own Account",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    ) == DialogResult.No
                )
                    return;
            }

            var confirmResult = MessageBox.Show(
                "Are you sure you want to delete this user?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirmResult == DialogResult.No)
            {
                return;
            }
            db_handler.DeleteUser(active_user.user_id);

            MessageBox.Show(
                "The user has been successfully deleted.",
                "User Deleted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            if (active_user.user_id == (current_user == null ? 0 : current_user.user_id))
            {
                var login_form = new LoginPage();
                login_form.Closed += (s, args) => this.ParentForm.Close();
                this.ParentForm.Visible = false;
                login_form.Show();
            }

            active_user = null;
            txtUsername.Text = "";
            txtPassword.Text = "";
            txtName.Text = "";
            cboRole.SelectedIndex = -1;

            UpdateUsersList();
            txtUsername.Focus();
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (active_user == null)
                {
                    MessageBox.Show(
                        "Please select a user to update.",
                        "No User Selected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation
                    );
                    return;
                }
                active_user.username = txtUsername.Text;
                if (!UserModel.ValidatePassword(txtPassword.Text) || (txtPassword.ForeColor == Color.DimGray && txtPassword.Text == "(unchanged)"))
                {
                    MessageBox.Show(
                        "The password that you have entered is invalid. The password must be at least 8 characters long.",
                        "Invalid Password",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation
                    );
                    return;
                }

                active_user.userpass = PasswordHandler.SHA256(txtPassword.Text);
                active_user.full_name = txtName.Text;
                active_user.privilege =
                    cboRole.SelectedIndex == 0
                        ? UserModel.Privilege.User
                        : UserModel.Privilege.Admin;
                active_user.Save();
                MessageBox.Show(
                    "The user has been successfully updated.",
                    "User Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An error occurred while updating the user. Please try again.\n\n" + ex.Message,
                    "Error Updating User",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            UpdateUsersList(txtSearch.Text);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                UpdateUsersList(txtSearch.Text);
            }
        }
    }
}

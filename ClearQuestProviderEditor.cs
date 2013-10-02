using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Custom editor for the ClearQuest issue tracker provider.
    /// </summary>
    internal sealed class ClearQuestProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;

        private ValidatingTextBox txtDatabase;
        private ValidatingTextBox txtProjectEntity;
        private ValidatingTextBox txtProjectEntityNameField;

        private ValidatingTextBox txtIssueEntities;
        
        private TextBox txtReleaseField;
        private TextBox txtProjectField;
        private ValidatingTextBox txtTitleField;
        private ValidatingTextBox txtDescriptionField;
        private ValidatingTextBox txtStatusField;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearQuestProviderEditor"/> class.
        /// </summary>
        public ClearQuestProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            EnsureChildControls();
            BindToForm((ClearQuestProvider)extension);
        }

        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();

            return new ClearQuestProvider()
            {
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text,
                Database = this.txtDatabase.Text,
                ProjectEntity = this.txtProjectEntity.Text,
                ProjectEntityNameField = this.txtProjectEntityNameField.Text,
                IssueEntities = this.txtIssueEntities.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                ReleaseField = this.txtReleaseField.Text,
                ProjectField = this.txtProjectField.Text,
                TitleField = this.txtTitleField.Text,
                DescriptionField = this.txtDescriptionField.Text,
                StatusField = this.txtStatusField.Text
            };
        }

        protected override void CreateChildControls()
        {
            txtUserName = new ValidatingTextBox() { Width = 300, Required = true };
            txtPassword = new PasswordTextBox() { Width = 270 };
            txtDatabase = new ValidatingTextBox() { Width = 300, Required = true };
            txtProjectEntity = new ValidatingTextBox() { Width = 300, Required = true };
            txtProjectEntityNameField = new ValidatingTextBox() { Width = 300, Required = true };

            txtIssueEntities = new ValidatingTextBox()
            {
                Width = 300,
                Required = true,
                TextMode = TextBoxMode.MultiLine,
                Rows = 3
            };

            txtReleaseField = new ValidatingTextBox() { Width = 300 };
            txtProjectField = new ValidatingTextBox() { Width = 300 };
            txtTitleField = new ValidatingTextBox() { Width = 300, Required = true };
            txtDescriptionField = new ValidatingTextBox() { Width = 300, Required = true };
            txtStatusField = new ValidatingTextBox() { Width = 300, Required = true };

            BindToForm(new ClearQuestProvider());

            CUtil.Add(this,
                new FormFieldGroup("ClearQuest Database",
                    "Provide login information for a ClearQuest database.",
                    false,
                    new StandardFormField(
                        "Database:",
                        txtDatabase),
                    new StandardFormField(
                        "User Name:",
                        txtUserName),
                    new StandardFormField(
                        "Password:",
                        txtPassword)
                    ),
                new FormFieldGroup("Projects",
                    "Specify information about ClearQuest Project entities to display in BuildMaster.",
                    false,
                    new StandardFormField(
                        "Project Entity Name:",
                        txtProjectEntity),
                    new StandardFormField(
                        "\"Name\" Field on Project Entities:",
                        txtProjectEntityNameField)
                    ),
                new FormFieldGroup("Issues",
                    "Provide the names of ClearQuest entities to display as issues (one entity type per line).",
                    false,
                    new StandardFormField(
                        string.Empty,
                        txtIssueEntities)
                    ),
                new FormFieldGroup("Issue Fields",
                    "Provide the names of relevant fields on ClearQuest issue entities.",
                    false,
                    new StandardFormField(
                        "Title:",
                        txtTitleField),
                    new StandardFormField(
                        "Description:",
                        txtDescriptionField),
                    new StandardFormField(
                        "Status:",
                        txtStatusField),
                    new StandardFormField(
                        "Release:",
                        txtReleaseField),
                    new StandardFormField(
                        "Project:",
                        txtProjectField)
                    )
               );

            base.CreateChildControls();
        }

        private void BindToForm(ClearQuestProvider provider)
        {
            this.txtUserName.Text = provider.UserName ?? string.Empty;
            this.txtPassword.Text = provider.Password ?? string.Empty;
            this.txtDatabase.Text = provider.Database ?? string.Empty;
            this.txtProjectEntity.Text = provider.ProjectEntity ?? string.Empty;
            this.txtProjectEntityNameField.Text = provider.ProjectEntityNameField ?? string.Empty;
            this.txtIssueEntities.Text = string.Join(Environment.NewLine, provider.IssueEntities ?? new[] { string.Empty });
            this.txtReleaseField.Text = provider.ReleaseField ?? string.Empty;
            this.txtProjectField.Text = provider.ProjectField ?? string.Empty;
            this.txtTitleField.Text = provider.TitleField ?? string.Empty;
            this.txtDescriptionField.Text = provider.DescriptionField ?? string.Empty;
            this.txtStatusField.Text = provider.StatusField ?? string.Empty;
        }
    }
}

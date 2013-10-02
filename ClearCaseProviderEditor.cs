using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Custom editor for the ClearCase provider.
    /// </summary>
    internal sealed class ClearCaseProviderEditor : ProviderEditorBase
    {
        private SourceControlFileFolderPicker txtExePath;
        private ValidatingTextBox txtViewPath;
        private TextBox txtBranch;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearCaseProviderEditor"/> class.
        /// </summary>
        public ClearCaseProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            EnsureChildControls();

            var provider = (ClearCaseProvider)extension;

            this.txtExePath.Text = provider.ExePath = Util.CoalesceStr(provider.ExePath, @"C:\Program Files\IBM\RationalSDLC\ClearCase\bin\cleartool.exe");
            this.txtViewPath.Text = provider.ViewPath ?? string.Empty;
            this.txtBranch.Text = Util.CoalesceStr(provider.BranchName, "main");
        }
        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();

            return new ClearCaseProvider()
            {
                ExePath = this.txtExePath.Text,
                ViewPath = this.txtViewPath.Text,
                BranchName = this.txtBranch.Text
            };
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation
        /// to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtExePath = new SourceControlFileFolderPicker()
            {
                Width = 300,
                Required = true,
                ServerId = this.EditorContext.ServerId
            };

            this.txtViewPath = new ValidatingTextBox()
            {
                Width = 300,
                Required = true
            };

            this.txtBranch = new TextBox()
            {
                Width = 300
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Executable Path",
                    "The path of the ClearCase command-line tool (cleartool.exe on Windows).",
                    false,
                    new StandardFormField(
                        "Executable Path:",
                        this.txtExePath
                        )
                    ),
                new FormFieldGroup(
                    "View Path",
                    "The path of BuildMaster's ClearCase snapshot view. This directory must already be created on servers where ClearCase actions are run. NOTE: BuildMaster will alter the configspec of this view.",
                    false,
                    new StandardFormField(
                        "View Path:",
                        this.txtViewPath
                        )
                    ),
                new FormFieldGroup(
                    "Branch",
                    "The name of a ClearCase branch to work against.",
                    false,
                    new StandardFormField(
                        "Branch:",
                        this.txtBranch
                        )
                    )
                );
        }
    }
}

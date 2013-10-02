using System;
using System.Collections.Generic;
using System.Data;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Connects to the Rational ClearQuest issue tracking system.
    /// </summary>
    [ProviderProperties("Rational ClearQuest", "Supports Rational ClearQuest 7.1 and later; requires ClearQuest client to be installed.")]
    [CustomEditor(typeof(ClearQuestProviderEditor))]
    internal sealed class ClearQuestProvider : IssueTrackingProviderBase, ICategoryFilterable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClearQuestProvider"/> class.
        /// </summary>
        public ClearQuestProvider()
        {
            this.ProjectEntity = "Project";
            this.IssueEntities = new[] { "defect" };
            this.ReleaseField = "planned_release";
            this.ProjectField = "project";
            this.ProjectEntityNameField = "name";
            this.TitleField = "headline";
            this.DescriptionField = "description";
            this.StatusField = "state";
        }

        /// <summary>
        /// Gets or sets the user name which will be used to log in to ClearQuest.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the password for the UserName property.
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the name of the ClearQuest database to connect to.
        /// </summary>
        [Persistent]
        public string Database { get; set; }
        /// <summary>
        /// Gets or sets the name of Project entities in the ClearQuest database.
        /// </summary>
        [Persistent]
        public string ProjectEntity { get; set; }
        /// <summary>
        /// Gets or sets the Name field on Project entities.
        /// </summary>
        [Persistent]
        public string ProjectEntityNameField { get; set; }
        /// <summary>
        /// Gets or sets the names of entities which will be listed as issues.
        /// </summary>
        [Persistent]
        public string[] IssueEntities { get; set; }
        /// <summary>
        /// Gets or sets the name of the Release field on issues.
        /// </summary>
        [Persistent]
        public string ReleaseField { get; set; }
        /// <summary>
        /// Gets or sets the name of the Project field on issues.
        /// </summary>
        [Persistent]
        public string ProjectField { get; set; }
        /// <summary>
        /// Gets or sets the name of the Title field on issues.
        /// </summary>
        [Persistent]
        public string TitleField { get; set; }
        /// <summary>
        /// Gets or sets the name of the Description field on issues.
        /// </summary>
        [Persistent]
        public string DescriptionField { get; set; }
        /// <summary>
        /// Gets or sets the name of the Status field on issues.
        /// </summary>
        [Persistent]
        public string StatusField { get; set; }

        /// <summary>
        /// Gets or sets the category id filter.
        /// </summary>
        /// <value>The category id filter.</value>
        public string[] CategoryIdFilter { get; set; }
        /// <summary>
        /// Gets an inheritor-defined array of category types.
        /// </summary>
        public string[] CategoryTypeNames
        {
            get { return new[] { "Project" }; }
        }

        /// <summary>
        /// Gets an array of <see cref="Issue"/> objects that are for the current
        /// release
        /// </summary>
        /// <param name="releaseNumber"></param>
        /// <returns></returns>
        public override Issue[] GetIssues(string releaseNumber)
        {
            var filter = new Dictionary<string, object>();

            if (this.CategoryIdFilter != null && this.CategoryIdFilter.Length > 0)
                filter.Add(this.ProjectField, this.CategoryIdFilter[0]);

            if (!string.IsNullOrEmpty(releaseNumber) && !string.IsNullOrEmpty(this.ReleaseField))
                filter.Add(this.ReleaseField, releaseNumber);

            var issues = new List<ClearQuestIssue>();

            using (var session = ClearQuestSession.Connect(this.UserName, this.Password, this.Database))
            {
                foreach (var issueEntity in this.IssueEntities)
                {
                    var table = session.RunFilteredQuery(issueEntity, filter, "id", this.TitleField, this.DescriptionField, this.StatusField);
                    foreach (DataRow row in table.Rows)
                    {
                        issues.Add(
                            new ClearQuestIssue(
                                row["id"].ToString(),
                                row[this.TitleField].ToString(),
                                row[this.DescriptionField].ToString(),
                                row[this.StatusField].ToString(),
                                releaseNumber));
                    }
                }
            }

            return issues.ToArray();
        }

        /// <summary>
        /// Determines if the specified issue is closed
        /// </summary>
        /// <param name="issue"></param>
        /// <returns></returns>
        public override bool IsIssueClosed(Issue issue)
        {
            return string.Equals(issue.IssueStatus, "Closed", StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// When implemented in a derived class, indicates whether the provider
        /// is installed and available for use in the current execution context
        /// </summary>
        /// <returns></returns>
        public override bool IsAvailable()
        {
            try
            {
                IsAvailable2();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// When implemented in a derived class, attempts to connect with the
        /// current configuration and, if not successful, throws a
        /// <see cref="ConnectionException"/>
        /// </summary>
        public override void ValidateConnection()
        {
            using (var session = ClearQuestSession.Connect(this.UserName, this.Password, this.Database))
            {
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Connects to the Rational ClearQuest issue tracking system";
        }

        /// <summary>
        /// Returns an array of all appropriate categories defined within the provider
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The nesting level (i.e. <see cref="CategoryBase.SubCategories"/>) can never be less than
        /// the length of <see cref="CategoryTypeNames"/>
        /// </remarks>
        public CategoryBase[] GetCategories()
        {
            using (var session = ClearQuestSession.Connect(this.UserName, this.Password, this.Database))
            {
                var projects = session.RunQuery(this.ProjectEntity, "dbid", this.ProjectEntityNameField);
                var categories = new List<ClearQuestCategory>();
                foreach (DataRow row in projects.Rows)
                    categories.Add(new ClearQuestCategory((string)row["dbid"], (string)row[this.ProjectEntityNameField]));

                return categories.ToArray();
            }
        }

        /// <summary>
        /// Tries to instantiate a Rational class.
        /// </summary>
        private static void IsAvailable2()
        {
            new ClearQuestOleServer.SessionClass();
        }
    }
}

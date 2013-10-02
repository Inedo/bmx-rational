using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Represents an issue in ClearQuest.
    /// </summary>
    [Serializable]
    internal sealed class ClearQuestIssue : IssueTrackerIssue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClearQuestIssue"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="title">The title.</param>
        /// <param name="description">The description.</param>
        /// <param name="status">The status.</param>
        /// <param name="release">The release.</param>
        public ClearQuestIssue(string id, string title, string description, string status, string release)
            : base(id, status, title, description, release)
        {
        }
    }
}

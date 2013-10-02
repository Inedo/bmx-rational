using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Rational
{
    /// <summary>
    /// Represents an issue category in ClearQuest.
    /// </summary>
    [Serializable]
    internal sealed class ClearQuestCategory : CategoryBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClearQuestCategory"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        public ClearQuestCategory(string id, string name)
            : base(id, name, null)
        {
        }
    }
}

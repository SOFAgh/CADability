﻿namespace CADability.UserInterface
{
    /// <summary>
    /// Seperator-Eintrag für TreeView
    /// </summary>

    public class SeperatorProperty : IShowPropertyImpl
    {
        private string labelText;
        public SeperatorProperty(string resourceId)
        {
            base.resourceId = resourceId;
        }
        #region IShowProperty Members
        public override ShowPropertyLabelFlags LabelType
        {
            get
            {
                return (ShowPropertyLabelFlags)0;
            }
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.EntryType"/>, 
        /// returns <see cref="ShowPropertyEntryType.Seperator"/>.
        /// </summary>
        public override ShowPropertyEntryType EntryType
        {
            get
            {
                return ShowPropertyEntryType.Seperator;
            }
        }
        #endregion
    }
}

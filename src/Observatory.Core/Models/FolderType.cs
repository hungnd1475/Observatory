using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Models
{
    /// <summary>
    /// Represents a type of a mail folder.
    /// </summary>
    public enum FolderType
    {
        /// <summary>
        /// Represents the root folder.
        /// </summary>
        Root,
        /// <summary>
        /// Represents the inbox.
        /// </summary>
        Inbox,
        /// <summary>
        /// Represents the folder containing sent items.
        /// </summary>
        SentItems,
        /// <summary>
        /// Represents the folder containing deleted items.
        /// </summary>
        DeletedItems,
        /// <summary>
        /// Represents the folder containing draft items.
        /// </summary>
        Drafts,
        /// <summary>
        /// Represents the archive.
        /// </summary>
        Archive,
        /// <summary>
        /// Represents other special folders.
        /// </summary>
        OtherSpecial,
        /// <summary>
        /// Represents a normal folder.
        /// </summary>
        Normal,
    }
}

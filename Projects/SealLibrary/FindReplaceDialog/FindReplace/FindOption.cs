#region Using Directives

using System;

#endregion Using Directives


namespace ScintillaNET_FindReplaceDialog
{
    /// <summary>
    ///     Controls find behavior for non-regular expression searches
    /// </summary>
    public enum FindOption
    {
        /// <summary>
        ///     Find must match the whole word
        /// </summary>
        WholeWord = 2,

        /// <summary>
        ///     Find must match the case of the expression
        /// </summary>
        MatchCase = 4,

        /// <summary>
        ///     Only match the _start of a word
        /// </summary>
        WordStart = 0x00100000,

        /// <summary>
        ///     Not used in ScintillaNET
        /// </summary>
        RegularExpression = 0x00200000,

        /// <summary>
        ///     Not used in ScintillaNET
        /// </summary>
        Posix = 0x00400000,
    }
}

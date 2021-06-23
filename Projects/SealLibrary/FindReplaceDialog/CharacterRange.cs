#region Using Directives

using System;
using System.Runtime.InteropServices;

#endregion Using Directives


namespace ScintillaNET_FindReplaceDialog
{
    /// <summary>
    /// Specifies a range of characters. If the cpMin and cpMax members are equal, the range is empty.
    /// The range includes everything if cpMin is 0 and cpMax is –1.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CharacterRange
    {
        /// <summary>
        /// Character position index immediately preceding the first character in the range.
        /// </summary>
        public int cpMin;
        /// <summary>
        /// Character position immediately following the last character in the range.
        /// </summary>
        public int cpMax;

        /// <summary>
        /// Specifies a range of characters. If the cpMin and cpMax members are equal, the range is empty.
        /// The range includes everything if cpMin is 0 and cpMax is –1.
        /// </summary>
        /// <param name="Min">The minimum, or start position.</param>
        /// <param name="Max">The maximum, or end position.</param>
        public CharacterRange(int Min, int Max)
        {
            this.cpMin = Min;
            this.cpMax = Max;
        }
    }
}

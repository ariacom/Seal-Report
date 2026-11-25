//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//

namespace Seal.Model
{
    /// <summary>
    /// Model of the 'Upload Check Script' of the server configuration: executed before a file uploaded in a repository folder from the Web Report Server
    /// is written. The script sets Refusal to refuse the file (allowed extensions, size, quota of the user...): nothing is written and the message is shown.
    /// </summary>
    public class UploadCheck
    {
        /// <summary>
        /// Current security, gives access to the repository
        /// </summary>
        public SealSecurity Security;
        /// <summary>
        /// User uploading the file
        /// </summary>
        public SecurityUser User;
        /// <summary>
        /// Repository path of the destination folder, as shown in the Web Report Server (e.g. \Reports or \Personal\Folder)
        /// </summary>
        public string FolderPath;
        /// <summary>
        /// Full path of the file to write (unique name in the folder)
        /// </summary>
        public string FilePath;
        /// <summary>
        /// Name of the file as sent by the browser, without folder
        /// </summary>
        public string FileName;
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long Size;
        /// <summary>
        /// First bytes of the file (4 096 at most), to check its nature (e.g. a spreadsheet starts with PK)
        /// </summary>
        public byte[] Header;
        /// <summary>
        /// Set by the script to refuse the file: message shown to the user
        /// </summary>
        public string Refusal;
    }
}

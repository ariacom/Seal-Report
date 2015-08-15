//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Seal.Helpers
{
    public class CryptoHelper
    {
        private static string TripleDESVector = "qOiä+-?$"; //8 chars
        public static string EncryptTripleDES(string text, string key)
        {
            //Expect an ANSI key of 24 chars...
            string result = "";
            try
            {
                if (string.IsNullOrEmpty(text)) return result;

                TripleDESCryptoServiceProvider crypto = new TripleDESCryptoServiceProvider();
                crypto.Key = Encoding.Default.GetBytes(key.Substring(0, 24));
                crypto.IV = Encoding.Default.GetBytes(TripleDESVector);

                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, crypto.CreateEncryptor(), CryptoStreamMode.Write);
                StreamWriter sw = new StreamWriter(cs);
                sw.Write(text);
                sw.Close();
                cs.Close();
                result = Convert.ToBase64String(ms.ToArray());
            }
            catch { }

            return result;
        }

        public static string DecryptTripleDES(string text, string key)
        {
            string result = "";
            try
            {
                if (string.IsNullOrEmpty(text)) return result;

                TripleDESCryptoServiceProvider crypto = new TripleDESCryptoServiceProvider();
                crypto.Key = Encoding.Default.GetBytes(key.Substring(0, 24));
                crypto.IV = Encoding.Default.GetBytes(TripleDESVector);

                MemoryStream ms = new MemoryStream(System.Convert.FromBase64String(text));
                CryptoStream cs = new CryptoStream(ms, crypto.CreateDecryptor(), CryptoStreamMode.Read);
                StreamReader sr = new StreamReader(cs);
                result = sr.ReadToEnd();
                sr.Close();
                cs.Close();
            }
            catch { }
            return result;
        }

    }
}

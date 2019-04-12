//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Seal.Helpers
{
    public class CryptoHelper
    {
        private static string TripleDESVector = "qOiä+-?$"; //8 chars

        static TripleDESCryptoServiceProvider getCryptoServiceProvider(string key)
        {
            TripleDESCryptoServiceProvider crypto = new TripleDESCryptoServiceProvider();
            crypto.Key = Encoding.Default.GetBytes(key + "123456789").Take(crypto.KeySize / 8).ToArray();
            crypto.IV = Encoding.Default.GetBytes(TripleDESVector + "123456789").Take(crypto.BlockSize / 8).ToArray();
            return crypto;
        }

        public static string EncryptTripleDES(string text, string key)
        {
            //Expect an ANSI key of 24 chars...
            string result = "";
            if (string.IsNullOrEmpty(text)) return result;

            var crypto = getCryptoServiceProvider(key);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, crypto.CreateEncryptor(), CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(cs);
            sw.Write(text);
            sw.Close();
            cs.Close();
            result = Convert.ToBase64String(ms.ToArray());

            return result;
        }

        public static string DecryptTripleDES(string text, string key)
        {
            string result = "";
            if (string.IsNullOrEmpty(text)) return result;

            var crypto = getCryptoServiceProvider(key);
            MemoryStream ms = new MemoryStream(System.Convert.FromBase64String(text));
            CryptoStream cs = new CryptoStream(ms, crypto.CreateDecryptor(), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cs);
            result = sr.ReadToEnd();
            sr.Close();
            cs.Close();
            return result;
        }

    }
}

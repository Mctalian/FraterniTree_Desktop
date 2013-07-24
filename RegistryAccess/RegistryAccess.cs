using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace RegistryAccess
{
    public static class RegAccess
    {
        private static string SOFTWARE_KEY = "Software";
        public static string COMPANY_NAME = "";
        public static string APPLICATION_NAME = "";

        public static string GetStringRegistryValue(string key, string defaultValue, string subFolder = null)
        {
            RegistryKey rkCompany;
            RegistryKey rkApplication;
            RegistryKey rkSubDir;
            rkCompany = Registry.CurrentUser
                                .OpenSubKey(SOFTWARE_KEY, false)
                                .OpenSubKey(COMPANY_NAME, false);
            if (rkCompany != null)
            {
                rkApplication = rkCompany.OpenSubKey(APPLICATION_NAME, true);
                if (rkApplication != null)
                {
                    if (subFolder == null)
                    {
                        foreach (string sKey in rkApplication.GetValueNames())
                        {
                            if (sKey == key)
                            {
                                return (string)rkApplication.GetValue(sKey);
                            }
                        }
                    }
                    else
                    {
                        rkSubDir = rkApplication.OpenSubKey(subFolder);
                        if (rkSubDir != null)
                        {
                            foreach (string sKey in rkSubDir.GetValueNames())
                            {
                                if (sKey == key)
                                {
                                    return (string)rkSubDir.GetValue(sKey);
                                }
                            }
                        }
                    }
                }
            }
            return defaultValue;
        }
        
        // Method for storing a Registry Value.
        public static void SetStringRegistryValue(string key, string stringValue, string subFolder = null)
        {
            RegistryKey rkSoftware;
            RegistryKey rkCompany;
            RegistryKey rkApplication;
            RegistryKey rkSubDir;
            rkSoftware = Registry.CurrentUser.OpenSubKey(SOFTWARE_KEY, true);
            rkCompany = rkSoftware.CreateSubKey(COMPANY_NAME);
            if (rkCompany != null)
            {
                rkApplication = rkCompany.CreateSubKey(APPLICATION_NAME);
                if (rkApplication != null)
                {
                    if (subFolder == null)
                    {
                        rkApplication.SetValue(key, stringValue);
                    }
                    else
                    {
                        rkSubDir = rkApplication.CreateSubKey(subFolder);
                        if (rkSubDir != null)
                        {
                            rkSubDir.SetValue(key, stringValue);
                        }
                    }
                }
            }
        }

        public static string[] GetAllSubKeys(string subFolder = null)
        {
            RegistryKey rkCompany;
            RegistryKey rkApplication;
            RegistryKey rkSubDir;
            rkCompany = Registry.CurrentUser
                                .OpenSubKey(SOFTWARE_KEY, false)
                                .OpenSubKey(COMPANY_NAME, false);
            if (rkCompany != null)
            {
                rkApplication = rkCompany.OpenSubKey(APPLICATION_NAME, true);
                if (rkApplication != null)
                {
                    if (subFolder == null)
                    {
                        return rkApplication.GetSubKeyNames();
                    }
                    else
                    {
                        rkSubDir = rkApplication.OpenSubKey(subFolder);
                        if (rkSubDir != null)
                        {
                            return rkSubDir.GetSubKeyNames();
                        }
                    }
                }
            }
            return null;
        }
    }
}

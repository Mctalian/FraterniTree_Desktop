
using System;
using System.Drawing.Imaging;
using System.Resources;
using FraterniTree.Enums;

namespace FraterniTree
{
    public static class Util
    {
        private static readonly ResourceManager ResourceManager = Properties.Resources.ResourceManager;

        public const int DefaultYear = 1920;
        public const string DefaultInitiationTerm = "Fall";
        public const string DefaultFirstName = "Robert";
        public const string DefaultLastName = "Mctalian";

        public static string GetLocalizedString( string resourceName )
        {
            if( string.IsNullOrEmpty(resourceName) ) throw new ArgumentOutOfRangeException();
            return ResourceManager.GetString( resourceName );
        }

        public static bool ConvertStringToBool( string input )
        {
            if( input == null ) return false;

            switch (input.ToUpper())
            {
                case "YES":
                case "Y":
                case "TRUE":
                case "T":
                case "1":
                    return true;
                default:
                    return false;
            }
        }

        public static ImageFormat GetImageFormatFromFileExtension( string fileExtension )
        {
            switch (fileExtension)
            {
                case ".png":
                    return ImageFormat.Png;
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".gif":
                    return ImageFormat.Gif;
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".jfif":
                    return ImageFormat.Jpeg;
                case ".tif":
                case ".tiff":
                    return ImageFormat.Tiff;
                default:
                    return ImageFormat.Png;
            }
        }

        public static InitiationTerm StringToInitiationTerm(string stringRepresentation)
        {
            if( string.IsNullOrEmpty( stringRepresentation ) ) throw new Exception("The string cannot be empty or null.");

            return (InitiationTerm) Enum.Parse(typeof(InitiationTerm), stringRepresentation);
        }
        
        
        

    }
}

using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FraterniTree.UserInterface
{

    internal partial class AboutCompanyDialog : Form
    {
        public AboutCompanyDialog( )
        {
            InitializeComponent();
            Text = string.Format( Util.GetLocalizedString("About"), AssemblyTitle );
            labelProductName.Text = AssemblyProduct;
            labelVersion.Text = string.Format( Util.GetLocalizedString("Version"), AssemblyVersion );
            labelCopyright.Text = AssemblyCopyright;
            labelCompanyName.Text = AssemblyCompany;
            textBoxDescription.Text = AssemblyDescription;
        }

        #region Assembly Attribute Accessors

        private static string AssemblyTitle
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyTitleAttribute), false );
                if( attributes.Length > 0 )
                {
                    var titleAttribute = (AssemblyTitleAttribute) attributes[0];
                    if( titleAttribute.Title != string.Empty )
                    {
                        return titleAttribute.Title;
                    }
                }

                return Path.GetFileNameWithoutExtension( Assembly.GetExecutingAssembly().CodeBase );
            }
        }

        private static string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        private static string AssemblyDescription
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyDescriptionAttribute), false );
                
                if( attributes.Length == 0 ) return string.Empty; 
                
                return ((AssemblyDescriptionAttribute) attributes[0]).Description;
            }
        }

        private static string AssemblyProduct
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyProductAttribute), false );

                if( attributes.Length == 0 ) return string.Empty; 
                
                return ((AssemblyProductAttribute) attributes[0]).Product;
            }
        }

        private static string AssemblyCopyright
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyCopyrightAttribute), false );
                
                if( attributes.Length == 0 ) return string.Empty; 

                return ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
            }
        }

        private static string AssemblyCompany
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyCompanyAttribute), false );

                if( attributes.Length == 0 ) return string.Empty;

                return ((AssemblyCompanyAttribute) attributes[0]).Company;
            }
        }

        #endregion
    }

}
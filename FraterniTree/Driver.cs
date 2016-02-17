using System;
using System.Windows.Forms;
using FraterniTree.UserInterface;

namespace FraterniTree
{

    internal static class Driver
    {

        [STAThread]
        private static void Main( )
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            Application.Run( new FamilyTreeForm() );
        }

    }

}
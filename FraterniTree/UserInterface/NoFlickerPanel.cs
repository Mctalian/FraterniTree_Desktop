using System.Windows.Forms;

namespace FraterniTree.UserInterface
{

    public class NoFlickerPanel : Panel
    {
        public NoFlickerPanel( )
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true );
        }
    }

}
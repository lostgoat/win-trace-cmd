using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WinTraceCmd
{
    class Output
    {
        static public TextBlock LogBlock { get; set; } = null;
        static public ScrollViewer LogScrollviewer { get; set; } = null;
        
        static public void Print( string msg )
        {
            if( LogBlock != null )
            {
                LogBlock.Text += msg + "\n";

                if ( LogScrollviewer != null )
                {
                    LogScrollviewer.ScrollToBottom();
                }
            }
        }

        static public void Printf( string format, params object[] args )
        {
            Print( String.Format( format, args ) );
        }
    }
}

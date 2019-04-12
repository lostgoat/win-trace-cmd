using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinTraceCmd
{
    class Output
    {
        static public TextBlock LogBlock { get; set; } = null;
        static public ScrollViewer LogScrollviewer { get; set; } = null;
        
        static public void Print( string msg )
        {
            Application.Current.Dispatcher.BeginInvoke( new Action( () =>
            {
                if( LogBlock != null )
                {
                    LogBlock.Text += msg + "\n";

                    if( LogScrollviewer != null )
                    {
                        LogScrollviewer.ScrollToBottom();
                    }
                }
            } ));

            //Dispatcher.CurrentDispatcher.Invoke( new Action( () => { } ), DispatcherPriority.ContextIdle );
        }

        static public void Printf( string format, params object[] args )
        {
            Print( String.Format( format, args ) );
        }
    }
}

/**
 * MIT License
 *
 * Copyright (c) 2019 Valve Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WinTraceCmd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Output.LogBlock = LogBlock;
            Output.LogScrollviewer = LogScrollViewer;

            mEtlTracer = new EtlTracer( mConfig );
            EtlPathBox.Text = mConfig.EtlOutputFile;
            WdatPathBox.Text = mConfig.WdatOutputFile;
        }

        enum TraceState
        {
            Stop,
            Start
        }

        private TraceState mTraceState = TraceState.Stop;
        private Config mConfig = new Config();
        private EtlTracer mEtlTracer;

        private void SetTraceState( TraceState state )
        {
            string buttonContent = "";
            string stateMsg = "";

            switch( state )
            {
                case TraceState.Start:
                    buttonContent = "Stop Trace";
                    stateMsg = "( Tracing... )";
                    break;
                case TraceState.Stop:
                    buttonContent = "Start Trace";
                    stateMsg = "";
                    break;
            }

            TraceButton.Content = buttonContent;
            Title = "WinTraceCmd " + stateMsg;
            mTraceState = state;
        }

        private void OnStartTrace( object sender, RoutedEventArgs e )
        {
            Debug.Assert( mTraceState == TraceState.Stop );
            SetTraceState( TraceState.Start );

            mEtlTracer.StartTrace();
        }

        private void OnStopTrace( object sender, RoutedEventArgs e )
        {
            Debug.Assert( mTraceState == TraceState.Start );
            SetTraceState( TraceState.Stop );

            mEtlTracer.StopTrace();
        }

        private void OnEtlPathChanged( object sender, TextChangedEventArgs e )
        {
            mConfig.EtlOutputFile = EtlPathBox.Text;
        }

        private void OnEtlPathButtonClick( object sender, RoutedEventArgs e )
        {
            ChooseOutputFile( EtlPathBox, "etl" );
        }

        private void OnWdatPathChanged( object sender, TextChangedEventArgs e )
        {
            mConfig.WdatOutputFile = WdatPathBox.Text;
        }

        private void OnWdatPathButtonClick( object sender, RoutedEventArgs e )
        {
            ChooseOutputFile( WdatPathBox, "wdat" );
        }

        private bool ChooseOutputFile( TextBox pathBox, string ext )
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = String.Format( "{0} files (*.{0})|*.{0}|All files (*.*)|*.*", ext );
            saveDialog.FilterIndex = 1;

            var result = saveDialog.ShowDialog();
            switch( result )
            {
                case System.Windows.Forms.DialogResult.OK:
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    return false;
            }

            var file = saveDialog.FileName;
            pathBox.Text = file;
            pathBox.ToolTip = file;

            return true;
        }
    }
}

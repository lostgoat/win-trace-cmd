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
using System.IO;
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
            mWdatParser = new WdatParser( mConfig );
            EtlPathBox.Text = mConfig.EtlOutputFile;
            SteamVrPathBox.Text = mConfig.SteamVRPath;
            GpuvisPathBox.Text = mConfig.GpuvisPath;
            sThisWindow = this;
        }



        enum TraceState
        {
            Stop,
            Start,
            Processing
        }

        private TraceState mTraceState = TraceState.Stop;
        private Config mConfig = Config.LoadConfig();
        private EtlTracer mEtlTracer;
        private WdatParser mWdatParser;
        private Task mWdatTask = null;
        private static MainWindow sThisWindow = null;

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
                case TraceState.Processing:
                    buttonContent = "Processing...";
                    stateMsg = "( Processing... )";
                    break;
            }

            TraceButton.Content = buttonContent;
            Title = "WinTraceCmd " + stateMsg;
            mTraceState = state;
        }

        private void OnStartTrace( object sender, RoutedEventArgs e )
        {
            Debug.Assert( mTraceState == TraceState.Stop );

            // Wait for the previous job to finish processing
            if( mWdatTask != null )
            {
                mWdatTask.Wait();
            }

            if ( !mEtlTracer.StartTrace() )
            {
                Output.Print( "Error: failed to start trace" );
                return;
            }

            SetTraceState( TraceState.Start );
        }

        private void OnStopTrace( object sender, RoutedEventArgs e )
        {
            Debug.Assert( mTraceState == TraceState.Start );
            SetTraceState( TraceState.Processing );

            mEtlTracer.StopTrace();

            mWdatTask = mWdatParser.ParseEventsAsync();
        }

        private void OnProcessingComplete()
        {
            Debug.Assert( mTraceState == TraceState.Processing );
            SetTraceState( TraceState.Stop );

            if ( File.Exists( mConfig.GpuvisPath) )
            {
                Process.Start( mConfig.GpuvisPath, mConfig.WdatOutputFile );
            }
        }

        private void OnEtlPathChanged( object sender, TextChangedEventArgs e )
        {
            mConfig.EtlOutputFile = EtlPathBox.Text;
            mConfig.SaveConfig();
        }

        private void OnEtlPathButtonClick( object sender, RoutedEventArgs e )
        {
            ChooseOutputFile( EtlPathBox, "etl" );
        }

        private void OnSteamVrPathChanged( object sender, TextChangedEventArgs e )
        {
            mConfig.SteamVRPath = SteamVrPathBox.Text;
            mConfig.SaveConfig();
        }

        private void OnSteamVrPathButtonClick( object sender, RoutedEventArgs e )
        {
            ChooseOutputFile( SteamVrPathBox, "exe", false );
        }

        private void OnGpuvisPathChanged( object sender, TextChangedEventArgs e )
        {
            mConfig.GpuvisPath = GpuvisPathBox.Text;
            mConfig.SaveConfig();
        }

        private void OnGpuvisPathButtonClick( object sender, RoutedEventArgs e )
        {
            ChooseOutputFile( GpuvisPathBox, "exe", false );
        }

        private bool ChooseOutputFile( TextBox pathBox, string ext, bool confirmOverwrite = true )
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.OverwritePrompt = confirmOverwrite;
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

        public enum AppEvents
        {
            ProcessingComplete,
        }

        private static void ProcessEvents( AppEvents e )
        {
            switch( e )
            {
                case AppEvents.ProcessingComplete:
                    sThisWindow.OnProcessingComplete();
                    break;
            }
        }

        public static void RaiseEvent( AppEvents e )
        {
            Application.Current.Dispatcher.BeginInvoke( new Action( () =>
            {
                ProcessEvents( e );
            } ) );
        }
    }
}

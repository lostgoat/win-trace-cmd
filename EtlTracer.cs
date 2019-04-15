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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Session;
using System.Diagnostics;
using System.IO;

namespace WinTraceCmd
{
    class EtlTracer
    {
        private static readonly string kSessionName = "WinTraceCmdSession";
        private Config mConfig;
        private TraceEventSession mEtwSession;

        public EtlTracer( Config config )
        {
            mConfig = config;
        }

        private bool PatchManifest( string source, string dest, string vrcompositorPath )
        {
            if( !File.Exists( source ) || !File.Exists( vrcompositorPath ) )
            {
                return false;
            }

            string manifestTemplate = File.ReadAllText( source );
            manifestTemplate = manifestTemplate.Replace( "%%VRCOMPOSITORPATH%%", vrcompositorPath );
            File.WriteAllText( dest, manifestTemplate );

            return true;
        }

        private bool RegisterManifest( string path )
        {
            // register any potentially old manifests for this GUID
            Process.Start( "wevtutil.exe", "um " + path ).WaitForExit();

            // register the new manifest
            Process manRegisterProc = new Process();
            manRegisterProc.StartInfo.FileName = "wevtutil.exe";
            manRegisterProc.StartInfo.Arguments = "im " + path;
            manRegisterProc.StartInfo.UseShellExecute = false;
            manRegisterProc.StartInfo.RedirectStandardError = true;
            manRegisterProc.StartInfo.RedirectStandardOutput = true;
            manRegisterProc.Start();
            manRegisterProc.WaitForExit();

            Output.Print( manRegisterProc.StandardOutput.ReadToEnd() );
            Output.Print( manRegisterProc.StandardError.ReadToEnd() );

            return ( manRegisterProc.ExitCode == 0 );
        }

        private bool SetupManifest()
        {
            string vrManFilename = "steamvretwprovider.man";
            string compositorPath = mConfig.SteamVRPath;

            if( !File.Exists( compositorPath ) )
            {
                Output.Print( "Error: invalid path to vrcompositor '" + compositorPath + "'" );
                return false;
            }

            string vrFolder = Directory.GetParent( compositorPath ).ToString();
            string manTemplatePath = vrFolder + "\\..\\" + vrManFilename;
            string manPath = Path.GetTempPath() + vrManFilename;

            Output.Print( "Using SteamVR tracing manifest: " + manTemplatePath );
            if ( !PatchManifest( manTemplatePath, manPath, compositorPath ) )
            {
                Output.Print( "Error: failed to parse SteamVR ETW manifest: '" + manPath + "'" );
                return false;

            }

            if ( !RegisterManifest( manPath ) )
            {
                Output.Print( "Error: failed to register SteamVR ETW manifest: '" + manPath + "'" );
                return false;
            }

            return true;
        }

        public bool EnableProvider( Config.TraceProvider provider )
        {
            Output.Print( "Enabing provider " + provider.Guid );
            switch( provider.Type )
            {
                case Config.TraceProviderType.kernel:
                    break;
                case Config.TraceProviderType.user:
                    mEtwSession.EnableProvider( provider.Guid );
                    break;
                default:
                    Output.Print( "Error: Bad kernel provider type for " + provider.Guid.ToString() );
                    break;
            }

            return true;
        }

        public bool StartTrace()
        {
            Output.Print( "Start tracing to " + mConfig.EtlOutputFile );

            if ( !SetupManifest() )
            {
                Output.Print( "Possible fix: Most likely the path to vrcompositor.exe provided in the box above is invalid." );
                return false;
            }

            mEtwSession = new TraceEventSession( kSessionName, mConfig.EtlOutputFile );

            foreach ( var provider in mConfig.EtwProviders )
            {
                EnableProvider( provider );
            }

            return true;
        }

        public bool StopTrace()
        {
            mEtwSession.Dispose();
            mEtwSession.Stop();
            mEtwSession = null;

            Output.Print( "Finished tracing to " + mConfig.EtlOutputFile );

            return true;
        }
    }
}

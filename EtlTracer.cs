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

        public bool EnableProvider( Config.TraceProvider provider )
        {
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
            Output.Print( "Start Trace" );

            mEtwSession = new TraceEventSession( kSessionName, mConfig.EtlOutputFile );

            foreach ( var provider in mConfig.EtwProviders )
            {
                EnableProvider( provider );
            }

            return true;
        }

        public bool StopTrace()
        {
            Output.Print( "Stop Trace" );

            mEtwSession.Dispose();
            mEtwSession = null;

            return true;
        }
    }
}

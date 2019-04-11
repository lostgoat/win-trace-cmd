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

namespace WinTraceCmd
{
    class Config
    {
        // ETW provider GUIDs in which we are interested
        public static readonly Guid kSteamVRGuid = new Guid( "8f8f13b1-60eb-4b6a-a433-de86104115ac" );
        public static readonly Guid kDxgKrnlGuid = new Guid( "802ec45a-1e99-4b83-9920-87c98277ba9d" );

        // ETW providers as an array
        public Guid[] EtwProviders { get; set; } =
        {
            kSteamVRGuid,
            kDxgKrnlGuid
        };

        // Where to store the trace etl file
        public string EtlOutputFile { get; set; } = "%temp%/wintracecmd.etl";

        // Where to store wdat output file
        public string WdatOutputFile { get; set;  } = "%temp%/wintracecmd.wdat";
    }
}

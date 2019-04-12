using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;


namespace WinTraceCmd
{
    class WdatParser
    {
        // Only set this if you want all etl records to be present in the wdat file
        // WARNING, it'll produce very large files
        // Useful for searching for new GUID sources
        private static readonly bool kDumpAllEntries = false;

        // Any changes to the Wdat format require a version bump
        private static readonly string kWdatVersion = "1";

        private readonly Config mConfig;
        private StreamWriter mWdatFile;

        public WdatParser( Config config )
        {
            mConfig = config;
        }

        public void ParseEvents()
        {
            Output.Print( "Processing events into: " + mConfig.WdatOutputFile );
            mWdatFile = new StreamWriter( mConfig.WdatOutputFile );

            HeaderWdatEntry header = new HeaderWdatEntry();
            mWdatFile.WriteLine( header.ToString() );

            using( ETWTraceEventSource source = new ETWTraceEventSource( mConfig.EtlOutputFile ) )
            {

                ETWContextWdatEntry context = new ETWContextWdatEntry( source );
                mWdatFile.WriteLine( context.ToString() );

                source.Clr.All += Consume;
                source.Kernel.All += Consume;
                source.Dynamic.All += Consume;

                source.Process();
            }

            mWdatFile.Flush();
            mWdatFile.Close();
            mWdatFile = null;

            Output.Print( "Finished processing wdat events" );
            MainWindow.RaiseEvent( MainWindow.AppEvents.ProcessingComplete );
        }

        // Convert an ETW trace to a Wdat trace
        public async Task ParseEventsAsync()
        {
            await Task.Run( () => ParseEvents() );
        }

        // Consume an event to be added to the wdat
        public void Consume( TraceEvent data )
        {
            BaseWdatEntry wdatEntry = null;

            switch( data.ProviderGuid )
            {
                case Guid guid when( guid == Config.kSteamVRGuid ):
                    wdatEntry = new SteamVRWdatEntry( data );
                    break;
                case Guid guid when( guid == Config.kDxgKrnlGuid && data.EventName == "VSyncInterrupt" ):
                    wdatEntry = new VsyncWdatEntry( data );
                    break;
                default:
                    if( !kDumpAllEntries )
                        return;
                    wdatEntry = new InspectionWdatEntry( data );
                    break;
            }

            mWdatFile.WriteLine( wdatEntry.ToString() );
        }

        public void Flush()
        {
            mWdatFile.Flush();
        }

        // Entry
        private enum WdatEntryId
        {
            Header = 0,
            ETWContext = 1,
            SteamVR = 2,
            Vsync = 3,
            Inspection = 9999,
        }

        // All wdat file entries must be based on BaseWdatEntry
        abstract class BaseWdatEntry
        {
            private string mWdatString = "";

            public abstract WdatEntryId EntryId { get; }

            public BaseWdatEntry()
            {
                AddField( "id", ( (int)EntryId ).ToString() );
            }

            public override string ToString()
            {
                return mWdatString.TrimEnd();
            }

            public void AddField( string field, string val )
            {
                mWdatString += String.Format( "{0}=`{1}` ", field, val );
            }

            public string NormalizeBase( string n )
            {
                UInt64 parsed = 0;

                if( n.Contains( "x" ) )
                {
                    parsed = Convert.ToUInt64( n, 16 );
                }
                else
                {
                    parsed = UInt64.Parse( n, NumberStyles.AllowThousands );
                }

                return parsed.ToString();
            }
        }

        // An entry to dump all the data
        class InspectionWdatEntry : BaseWdatEntry
        {
            public override WdatEntryId EntryId { get; } = WdatEntryId.Inspection;

            public InspectionWdatEntry( TraceEvent data )
            {
                AddField( "provider", data.ProviderGuid.ToString() );
                AddField( "opcode", data.Opcode.ToString() );
                AddField( "data", data.ToString() );
            }
        }

        // A header so that we can detect when we make some wdat format changes
        class HeaderWdatEntry : BaseWdatEntry
        {
            public override WdatEntryId EntryId { get; } = WdatEntryId.Header;

            public HeaderWdatEntry()
            {
                AddField( "version", kWdatVersion );
            }
        }

        // Provides information that applies to all following event entries
        class ETWContextWdatEntry : BaseWdatEntry
        {
            public override WdatEntryId EntryId { get; } = WdatEntryId.ETWContext;

            public ETWContextWdatEntry( ETWTraceEventSource source )
            {
                AddField( "file", source.LogFileName );
                AddField( "os_version", source.OSVersion.ToString() );
                AddField( "num_cpu", source.NumberOfProcessors.ToString() );
                AddField( "start_time", source.SessionStartTime.Ticks.ToString() );
                AddField( "end_time", source.SessionEndTime.Ticks.ToString() );
            }
        }

        // Any event must provide this information
        abstract class EventWdatEntry : BaseWdatEntry
        {
            public EventWdatEntry( TraceEvent data )
            {
                AddField( "ts", data.TimeStamp.Ticks.ToString() );
                AddField( "ts_rms", data.TimeStampRelativeMSec.ToString() );
                AddField( "cpu", data.ProcessorNumber.ToString() );
                AddField( "pid", data.ProcessID.ToString() );
                AddField( "tid", data.ThreadID.ToString() );
                AddField( "pname", data.ProcessName );
            }
        }

        // A SteamVR event
        class SteamVRWdatEntry : EventWdatEntry
        {
            public override WdatEntryId EntryId { get; } = WdatEntryId.SteamVR;

            public SteamVRWdatEntry( TraceEvent data ) : base( data )
            {
                AddField( "vrevent", data.PayloadString( 0 ) );
            }
        }

        // A VSync event
        class VsyncWdatEntry : EventWdatEntry
        {
            public override WdatEntryId EntryId { get; } = WdatEntryId.Vsync;

            public VsyncWdatEntry( TraceEvent data ) : base( data )
            {
                AddField( "adapter", NormalizeBase( data.PayloadString( 0 ) ) );
                AddField( "display", NormalizeBase( data.PayloadString( 1 ) ) );
                AddField( "address", NormalizeBase( data.PayloadString( 2 ) ) );
            }
        }
    }
}

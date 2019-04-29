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
            if ( !mConfig.EnableWdat )
            {
                MainWindow.RaiseEvent( MainWindow.AppEvents.ProcessingComplete );
                return;
            }

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

            // In explore mode we just dump a single provider to the wdat in full
            // This is useful to begin understanding the output from a specific provider
            if( Config.kEnableTestExploreMode )
            {
                if( data.ProviderGuid != Config.kTestGuid )
                    return;

                mWdatFile.WriteLine( new InspectionWdatEntry( data ) );
                return;
            }

            switch( data.ProviderGuid )
            {
                case Guid guid when( guid == Config.kSteamVRGuid ):
                    wdatEntry = new SteamVRWdatEntry( data );
                    break;
                case Guid guid when( guid == Config.kDxcGuid ):
                    switch( (int) data.Task )
                    {
                        case VsyncWdatEntry.kTaskId:
                            wdatEntry = new VsyncWdatEntry( data );
                            break;
                        case QueuePacketWdatEntry.kTaskId:
                            wdatEntry = new InspectionWdatEntry( data );
                            break;
                    }
                    break;
            }

            if ( wdatEntry == null )
            {
                // We aren't interested in this event
                if( !kDumpAllEntries )
                    return;

                // A simple handler for unknown events
                wdatEntry = new InspectionWdatEntry( data );
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
            QueuePacket = 4,
            Inspection = 9999,
        }

        // All wdat file entries must be based on BaseWdatEntry
        abstract class BaseWdatEntry
        {
            private string mWdatString = "";

            public abstract WdatEntryId EntryId { get; }

            public BaseWdatEntry()
            {
                AddField( "id", EntryId.ToString( "d" ) );
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

            public string GetAllPayloads( TraceEvent data )
            {
                string msg = "";
                foreach( string name in data.PayloadNames )
                {
                    msg += String.Format( "{0}[{1}]='{2}' ", name, data.PayloadIndex( name ), data.PayloadStringByName( name ) );
                }
                return msg;
            }
        }

        // An entry to dump all the data
        class InspectionWdatEntry : EventWdatEntry
        {
            public override WdatEntryId EntryId { get; } = WdatEntryId.Inspection;

            public InspectionWdatEntry( TraceEvent data ) : base( data )
            {
                AddField( "guid", data.ProviderGuid.ToString() );
                AddField( "opcode", data.Opcode.ToString() );
                AddField( "data", GetAllPayloads(data) );
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
                AddField( "ename", data.EventName );
                AddField( "provider", data.ProviderName );
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

        // Stores some metadata for DxgKrnlEvent
        abstract class DxgKrnlEvent : EventWdatEntry
        {
            public const int kInfoOpcode = 0;
            public const int kStartOpcode = 1;
            public const int kStopOpcode = 2;

            public DxgKrnlEvent( TraceEvent data ) : base(data)
            {
            }
        }

        // A VSync event
        class VsyncWdatEntry : DxgKrnlEvent
        {
            public const int kTaskId = 10;

            public override WdatEntryId EntryId { get; } = WdatEntryId.Vsync;

            public VsyncWdatEntry( TraceEvent data ) : base( data )
            {
                AddField( "adapter", NormalizeBase( data.PayloadString( 0 ) ) );
                AddField( "display", NormalizeBase( data.PayloadString( 1 ) ) );
                AddField( "address", NormalizeBase( data.PayloadString( 2 ) ) );
            }
        }

        // A QueuePacket event
        class QueuePacketWdatEntry : DxgKrnlEvent
        {
            public const int kTaskId = 9;

            public override WdatEntryId EntryId { get; } = WdatEntryId.QueuePacket;

            public QueuePacketWdatEntry( TraceEvent data ) : base( data )
            {
                AddField( "opcode", data.Opcode.ToString( "d" ) );
                AddField( "ctx", NormalizeBase( data.PayloadString( 0 ) ) );
                AddField( "ptype", data.PayloadString( 1 ) );
                AddField( "seq", NormalizeBase( data.PayloadString( 2 ) ) );

                switch( data.Opcode )
                {
                    case Microsoft.Diagnostics.Tracing.TraceEventOpcode.Start:
                        int type = (int)data.PayloadValue( 1 );
                        // not all events have these fields ?
                        if( type == 100 )
                        {
                            AddField( "dmabufsize", NormalizeBase( data.PayloadString( 3 ) ) );
                            AddField( "alloclistsize", NormalizeBase( data.PayloadString( 4 ) ) );
                            AddField( "patchloclistsize", NormalizeBase( data.PayloadString( 5 ) ) );
                            AddField( "present", data.PayloadString( 6 ) );
                            AddField( "dmabuf", NormalizeBase( data.PayloadString( 7 ) ) );
                            AddField( "packet", NormalizeBase( data.PayloadString( 8 ) ) );
                            AddField( "afence", NormalizeBase( data.PayloadString( 9 ) ) );
                        }
                        break;
                    case Microsoft.Diagnostics.Tracing.TraceEventOpcode.Info:
                        // No extra fields
                        break;
                    case Microsoft.Diagnostics.Tracing.TraceEventOpcode.Stop:
                        AddField( "preempted", data.PayloadString( 3 ) );
                        AddField( "timedout", data.PayloadString( 4 ) );
                        AddField( "packet", NormalizeBase( data.PayloadString( 5 ) ) );
                        break;
                }
            }
        }
    }
}

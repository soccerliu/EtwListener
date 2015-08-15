// Downloaded from http://archive.msdn.microsoft.com/EventTraceWatcher/Release/ProjectReleases.aspx?ReleaseId=2333
// licensed under MSDN Code Gallery Licenses / Source code files are governed by the MICROSOFT PUBLIC LICENSE (Ms-PL)
//
// Note that a slightly different copy is also checked-in in other source depot locations
// - //depot/win8_gdr/base/fs/remotefs/srv/xperf/smbxparse/TraceEventInfoWrapper.cs#1 
// - //depot/win8_gdr/basetest/clientperf/diagnosis/perftrack/perftrackrealtime/PerfTrackRealTime/Interop/TraceEventInfoWrapper.cs#1  
// - etc.

//------------------------------------------------------------------------------
// Author: Daniel Vasquez Lopez
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;


namespace Samples.Eventing.Interop
{
    internal sealed class TraceEventInfoWrapper : IDisposable
    {

        /// <summary>
        /// Base address of the native TraceEventInfo structure.
        /// </summary>
        private IntPtr _address;

        /// <summary>
        /// Managed representation of the native TraceEventInfo structure.
        /// </summary>
        private TraceEventInfo _traceEventInfo;

        //
        // True if the event has a schema with well defined properties.
        //
        private bool _hasProperties;

        /// <summary>
        /// Marshalled array of EventPropertyInfo objects.
        /// </summary>
        private EventPropertyInfo[] _eventPropertyInfoArray;

        internal TraceEventInfoWrapper(EventRecord eventRecord)
        {
            Initialize(eventRecord);
        }

        ~TraceEventInfoWrapper()
        {
            ReleaseMemory();
        }

        internal string EventName
        {
            private set;
            get;
        }

        public void Dispose()
        {
            ReleaseMemory();
            GC.SuppressFinalize(this);
        }

        internal PropertyBag GetProperties(EventRecord eventRecord)
        {
            // We only support top level properties and simple types
            PropertyBag properties = new PropertyBag();

            //Console.WriteLine("USERDATA length " + eventRecord.UserDataLength);
            if (this._hasProperties)
            {

                UInt32 offset = 0;

                for (int i = 0; i < _traceEventInfo.TopLevelPropertyCount; i++)
                {
                    EventPropertyInfo info = _eventPropertyInfoArray[i];

                    // Read the current property name
                    string propertyName = Marshal.PtrToStringUni(new IntPtr(_address.ToInt64() + info.NameOffset));

                    object value;
                    string mapName;
                    UInt32 length = 0;
                    // deal with variable size parameters
                    if (info.Flags == PropertyFlags.ParamLength)
                    {
                        string sizePropertyName = Marshal.PtrToStringUni(new IntPtr(_address.ToInt64() + _eventPropertyInfoArray[info.LengthPropertyIndex].NameOffset));
                        length = (UInt32)properties[sizePropertyName];
                    }
                    IntPtr dataPtr = new IntPtr(eventRecord.UserData.ToInt64() + offset);

                    value = ReadPropertyValue(info, dataPtr, out mapName, ref length);

                    // If we have a map name, return both map name and map value as a pair.
                    if (!string.IsNullOrEmpty(mapName))
                    {
                        value = new KeyValuePair<string, object>(mapName, value);
                    }

                    offset += length;
                    properties.Add(propertyName, value);
                }

                if (offset < eventRecord.UserDataLength)
                {
                    //
                    // There is some extra information not mapped.
                    //
                    IntPtr dataPtr = new IntPtr(eventRecord.UserData.ToInt64() + offset);
                    UInt32 length = eventRecord.UserDataLength - offset;
                    byte[] array = new byte[length];

                    for (int index = 0; index < length; index++)
                    {
                        array[index] = Marshal.ReadByte(dataPtr, index);
                    }

                    properties.Add("__ExtraPayload", array);
                }
            }
            else
            {
                // NOTE: It is just a guess that this is an Unicode string
                string str = Marshal.PtrToStringUni(eventRecord.UserData);

                properties.Add("EventData", str);
            }

            return properties;
        }

        private void Initialize(EventRecord eventRecord)
        {
            int size = 0;
            const uint BufferTooSmall = 122;
            const uint ErrorlementNotFound = 1168;

            int error = NativeMethods.TdhGetEventInformation(ref eventRecord, 0, IntPtr.Zero, IntPtr.Zero, ref size);
            if (error == ErrorlementNotFound)
            {
                // Nothing else to do here.
                this._hasProperties = false;
                return;
            }
            this._hasProperties = true;

            if (error != BufferTooSmall)
            {
                throw new Win32Exception(error);
            }

            // Get the event information (schema)
            this._address = Marshal.AllocHGlobal(size);
            error = NativeMethods.TdhGetEventInformation(ref eventRecord, 0, IntPtr.Zero, _address, ref size);
            if (error != 0)
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            // Marshal the first part of the trace event information.
            _traceEventInfo = (TraceEventInfo)Marshal.PtrToStructure(this._address, typeof(TraceEventInfo));

            // Marshal the second part of the trace event information, the array of property info.
            int actualSize = Marshal.SizeOf(this._traceEventInfo);
            if (size != actualSize)
            {
                int structSize = Marshal.SizeOf(typeof(EventPropertyInfo));
                int itemsLeft = (size - actualSize) / structSize;

                this._eventPropertyInfoArray = new EventPropertyInfo[itemsLeft];
                long baseAddress = _address.ToInt64() + actualSize;
                for (int i = 0; i < itemsLeft; i++)
                {
                    IntPtr structPtr = new IntPtr(baseAddress + (i * structSize));
                    EventPropertyInfo info = (EventPropertyInfo)Marshal.PtrToStructure(structPtr, typeof(EventPropertyInfo));
                    this._eventPropertyInfoArray[i] = info;
                }
            }

            // Get the opcode name
            if (_traceEventInfo.OpcodeNameOffset > 0)
            {
                this.EventName = Marshal.PtrToStringUni(new IntPtr(_address.ToInt64() + _traceEventInfo.OpcodeNameOffset));
            }
        }

        private object ReadPropertyValue(EventPropertyInfo info, IntPtr dataPtr, out string mapName, ref UInt32 length)
        {

            if (length == 0)
            {
                length = info.LengthPropertyIndex;
            }

            if (info.NonStructTypeValue.MapNameOffset != 0)
            {
                mapName = Marshal.PtrToStringUni(new IntPtr(this._address.ToInt64() + info.NonStructTypeValue.MapNameOffset));
            }
            else
            {
                mapName = string.Empty;
            }

            switch (info.NonStructTypeValue.InType)
            {
                case TdhInType.Null:
                    break;
                case TdhInType.UnicodeString:
                    {
                        string str = Marshal.PtrToStringUni(dataPtr);
                        length = (UInt32)((str.Length + 1) * sizeof(char));
                        return str;
                    }
                case TdhInType.AnsiString:
                    {
                        string str = Marshal.PtrToStringAnsi(dataPtr);
                        length = (UInt32)(str.Length + 1);
                        return str;
                    }
                case TdhInType.Int8:
                    return (sbyte)Marshal.ReadByte(dataPtr);
                case TdhInType.UInt8:
                    return Marshal.ReadByte(dataPtr);
                case TdhInType.Int16:
                    return Marshal.ReadInt16(dataPtr);
                case TdhInType.UInt16:
                    return (UInt16)Marshal.ReadInt16(dataPtr);
                case TdhInType.Int32:
                    return Marshal.ReadInt32(dataPtr);
                case TdhInType.UInt32:
                    return (uint)Marshal.ReadInt32(dataPtr);
                case TdhInType.Int64:
                    return Marshal.ReadInt64(dataPtr);
                case TdhInType.UInt64:
                    return (ulong)Marshal.ReadInt64(dataPtr);
                case TdhInType.Float:
                    Single[] singleArray = new Single[1];
                    Marshal.Copy(dataPtr, singleArray, 0, 1);
                    return singleArray[0];
                //return ReadFloat(dataPtr);
                case TdhInType.Double:
                    return (double)Marshal.ReadInt64(dataPtr);
                case TdhInType.Boolean:
                    return (bool)(Marshal.ReadInt32(dataPtr) != 0);
                case TdhInType.Binary:
                    //length must come specified
                    byte[] data = new byte[length];
                    Marshal.Copy(dataPtr, data, 0, (int)length);
                    return data;
                case TdhInType.Guid:
                    return new Guid(
                           Marshal.ReadInt32(dataPtr),
                           Marshal.ReadInt16(dataPtr, 4),
                           Marshal.ReadInt16(dataPtr, 6),
                           Marshal.ReadByte(dataPtr, 8),
                           Marshal.ReadByte(dataPtr, 9),
                           Marshal.ReadByte(dataPtr, 10),
                           Marshal.ReadByte(dataPtr, 11),
                           Marshal.ReadByte(dataPtr, 12),
                           Marshal.ReadByte(dataPtr, 13),
                           Marshal.ReadByte(dataPtr, 14),
                           Marshal.ReadByte(dataPtr, 15)
                           );
                case TdhInType.Pointer:
                    return Marshal.ReadIntPtr(dataPtr);
                case TdhInType.FileTime:
                    return DateTime.FromFileTimeUtc(Marshal.ReadInt64(dataPtr));
                case TdhInType.SystemTime:
                    return DateTime.FromFileTime(Marshal.ReadInt64(dataPtr));
                case TdhInType.SID:
                    break;
                case TdhInType.HexInt32:
                    return (UInt32)Marshal.ReadInt32(dataPtr);
                case TdhInType.HexInt64:
                    return (UInt64)Marshal.ReadInt64(dataPtr);
                case TdhInType.CountedString:
                    break;
                case TdhInType.CountedAnsiString:
                    break;
                case TdhInType.ReversedCountedString:
                    break;
                case TdhInType.ReversedCountedAnsiString:
                    break;
                case TdhInType.NonNullTerminatedString:
                    break;
                case TdhInType.NonNullTerminatedAnsiString:
                    break;
                case TdhInType.UnicodeChar:
                    break;
                case TdhInType.AnsiChar:
                    break;
                case TdhInType.SizeT:
                    break;
                case TdhInType.HexDump:
                    break;
                case TdhInType.WbemSID:
                    break;
                default:
                    Debugger.Break();
                    break;
            }

            throw new NotSupportedException();
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void ReleaseMemory()
        {
            if (this._address != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this._address);
                this._address = IntPtr.Zero;
            }
        }

    }
}
﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;
using AudioSwitcher.AudioApi.Hooking.ComObjects;
using EasyHook;

namespace AudioSwitcher.AudioApi.Hooking
{
    public class DefaultDeviceHook : IHook, IDisposable
    {
        private readonly Func<DataFlow, Role, string> _systemDeviceId;
        private readonly int _processId;
        private string _channelName;

        public delegate void OnErrorHandler(int processId, Exception exception);

        public event OnErrorHandler OnError;

        public bool IsHooked
        {
            get;
            private set;
        }

        public DefaultDeviceHook(int processId, Func<DataFlow, Role, string> systemDeviceId)
        {
            _processId = processId;
            _systemDeviceId = systemDeviceId;
        }

        public void Hook()
        {
            if (IsHooked)
                return;

            var ri = new RemoteInterface
            {
                //Wrap the target delegate in our own delegate for reference safety
                SystemId = (x, y) => _systemDeviceId(x, y),
                Unload = () => !IsHooked,
                ErrorHandler = RaiseOnError
            };

            RemoteHooking.IpcCreateServer(ref _channelName, WellKnownObjectMode.Singleton, ri);

            RemoteHooking.Inject(
                _processId,
                InjectionOptions.DoNotRequireStrongName,
                typeof(IMMDeviceEnumerator).Assembly.Location,
                typeof(IMMDeviceEnumerator).Assembly.Location,
                _channelName);

            IsHooked = true;
        }

        private void RaiseOnError(int processId, Exception exception)
        {
            if (OnError != null)
                OnError(processId, exception);
        }

        public void UnHook()
        {
            if (!IsHooked)
                return;

            IsHooked = false;
        }

        public class RemoteInterface : MarshalByRefObject, IRemoteHook
        {

            public Func<DataFlow, Role, string> SystemId
            {
                get;
                set;
            }

            public Func<bool> Unload
            {
                get;
                set;
            }

            public Action<int, Exception> ErrorHandler
            {
                get;
                set;
            }

            public string GetDefaultDevice(DataFlow dataFlow, Role role)
            {
                if (SystemId == null)
                    return String.Empty;

                return SystemId(dataFlow, role);
            }

            public bool CanUnload()
            {
                if (Unload == null)
                    return true;

                return Unload();
            }

            public void ReportError(int processId, Exception e)
            {
                if (ErrorHandler != null)
                    ErrorHandler(processId, e);
            }
        }

        public class EntryPoint : IEntryPoint
        {
            public readonly RemoteInterface Interface;

            [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = false)]
            [return: MarshalAs(UnmanagedType.U4)]
            public delegate int DGetDefaultAudioEndpoint(IMMDeviceEnumerator self, DataFlow dataFlow, Role role, out IntPtr ppEndpoint);

            public EntryPoint(RemoteHooking.IContext inContext, string inChannelName)
            {
                Interface = RemoteHooking.IpcConnectClient<RemoteInterface>(inChannelName);
            }

            public void Run(RemoteHooking.IContext inContext, string inChannelName)
            {
                //Create the DefaultDevice Hook
                var cci = new ComClassQuery.ComClassInfo(typeof(MMDeviceEnumeratorComObject),
                    typeof(IMMDeviceEnumerator), "GetDefaultAudioEndpoint");
                ComClassQuery.Query(cci);

                var hook = LocalHook.Create(cci.FunctionPointer, new DGetDefaultAudioEndpoint(GetDefaultAudioEndpoint), this);
                hook.ThreadACL.SetExclusiveACL(new int[] { });

                try
                {
                    while (!Interface.CanUnload())
                    {
                        Thread.Sleep(1000);
                    }

                    hook.Dispose();
                }
                catch (Exception e)
                {
                    try
                    {
                        Interface.ReportError(RemoteHooking.GetCurrentProcessId(), e);
                    }
                    catch
                    {
                        //.NET Remoting timeout etc...
                    }
                }
                finally
                {
                    hook.Dispose();
                }

            }

            private static int GetDefaultAudioEndpoint(IMMDeviceEnumerator self, DataFlow dataflow, Role role, out IntPtr ppendpoint)
            {
                var entryPoint = HookRuntimeInfo.Callback as EntryPoint;

                if (entryPoint == null || entryPoint.Interface == null)
                    return self.GetDefaultAudioEndpoint(dataflow, role, out ppendpoint);

                var remoteInterface = entryPoint.Interface;

                try
                {
                    var devId = remoteInterface.GetDefaultDevice(dataflow, role);
                    return self.GetDevice(devId, out ppendpoint);
                }
                catch (Exception ex)
                {
                    remoteInterface.ReportError(RemoteHooking.GetCurrentProcessId(), ex);
                    //Something failed so return the actual default device
                    return self.GetDefaultAudioEndpoint(dataflow, role, out ppendpoint);
                }
            }
        }

        public void Dispose()
        {
            UnHook();
        }
    }
}

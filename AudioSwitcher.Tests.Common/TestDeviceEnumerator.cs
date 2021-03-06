﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AudioSwitcher.AudioApi;

namespace AudioSwitcher.Tests.Common
{
    public class TestDeviceEnumerator : IDeviceEnumerator<TestDevice>
    {
        private readonly ConcurrentBag<TestDevice> _devices;
        private Guid _defaultPlaybackCommDeviceId;
        private Guid _defaultPlaybackDeviceId;
        private Guid _defaultCaptureCommDeviceId;
        private Guid _defaultCaptureDeviceId;


        public TestDeviceEnumerator(int numPlaybackDevices, int numCaptureDevices)
        {
            _devices = new ConcurrentBag<TestDevice>();

            for (int i = 0; i < numPlaybackDevices; i++)
            {
                var id = Guid.NewGuid();
                var dev = new TestDevice(id, DeviceType.Playback, this);
                _devices.Add(dev);
            }

            for (int i = 0; i < numCaptureDevices; i++)
            {
                var id = Guid.NewGuid();
                var dev = new TestDevice(id, DeviceType.Capture, this);
                _devices.Add(dev);
            }
        }

        public AudioController AudioController { get; set; }

        public TestDevice DefaultPlaybackDevice
        {
            get { return _devices.FirstOrDefault(x => x.Id == _defaultPlaybackDeviceId); }
        }

        public TestDevice DefaultCommunicationsPlaybackDevice
        {
            get { return _devices.FirstOrDefault(x => x.Id == _defaultPlaybackCommDeviceId); }
        }

        public TestDevice DefaultCaptureDevice
        {
            get { return _devices.FirstOrDefault(x => x.Id == _defaultCaptureDeviceId); }
        }

        public TestDevice DefaultCommunicationsCaptureDevice
        {
            get { return _devices.FirstOrDefault(x => x.Id == _defaultCaptureCommDeviceId); }
        }

        public TestDevice GetDevice(Guid id)
        {
            return _devices.FirstOrDefault(x => x.Id == id);
        }

        public TestDevice GetDefaultDevice(DeviceType deviceType, Role eRole)
        {
            switch (deviceType)
            {
                case DeviceType.Capture:
                    if (eRole == Role.Console || eRole == Role.Multimedia)
                        return DefaultCaptureDevice;

                    return DefaultCommunicationsCaptureDevice;
                case DeviceType.Playback:
                    if (eRole == Role.Console || eRole == Role.Multimedia)
                        return DefaultPlaybackDevice;

                    return DefaultCommunicationsPlaybackDevice;
            }

            return null;
        }

        public IEnumerable<TestDevice> GetDevices(DeviceType deviceType, DeviceState eRole)
        {
            return _devices.Where(x =>
                (x.DeviceType == deviceType || deviceType == DeviceType.All)
                && (x.State & eRole) > 0
                );
        }

        public bool SetDefaultDevice(TestDevice dev)
        {
            if (dev.IsPlaybackDevice)
            {
                _defaultPlaybackDeviceId = dev.Id;
                return true;
            }

            if (dev.IsCaptureDevice)
            {
                _defaultCaptureDeviceId = dev.Id;
                return true;
            }

            return false;
        }

        public bool SetDefaultCommunicationsDevice(TestDevice dev)
        {
            if (dev.IsPlaybackDevice)
            {
                _defaultPlaybackCommDeviceId = dev.Id;
                return true;
            }

            if (dev.IsCaptureDevice)
            {
                _defaultCaptureCommDeviceId = dev.Id;
                return true;
            }

            return false;
        }

        IDevice IDeviceEnumerator.DefaultPlaybackDevice
        {
            get { return DefaultPlaybackDevice; }
        }

        IDevice IDeviceEnumerator.DefaultCommunicationsPlaybackDevice
        {
            get { return DefaultCommunicationsPlaybackDevice; }
        }

        IDevice IDeviceEnumerator.DefaultCaptureDevice
        {
            get { return DefaultCaptureDevice; }
        }

        IDevice IDeviceEnumerator.DefaultCommunicationsCaptureDevice
        {
            get { return DefaultCommunicationsCaptureDevice; }
        }

        public event AudioDeviceChangedHandler AudioDeviceChanged;

        IDevice IDeviceEnumerator.GetDevice(Guid id)
        {
            return GetDevice(id);
        }

        IDevice IDeviceEnumerator.GetDefaultDevice(DeviceType deviceType, Role eRole)
        {
            return GetDefaultDevice(deviceType, eRole);
        }

        IEnumerable<IDevice> IDeviceEnumerator.GetDevices(DeviceType deviceType, DeviceState state)
        {
            return GetDevices(deviceType, state);
        }

        public bool SetDefaultDevice(IDevice dev)
        {
            var device = dev as TestDevice;
            if (device != null)
                return SetDefaultDevice(device);

            return false;
        }

        public bool SetDefaultCommunicationsDevice(IDevice dev)
        {
            var device = dev as TestDevice;
            if (device != null)
                return SetDefaultCommunicationsDevice(device);

            return false;
        }
    }
}

﻿namespace AudioSwitcher.AudioApi.Isolated
{
    public class IsolatedAudioController : AudioController<IsolatedDevice>
    {
        public IsolatedAudioController()
            : base(new DebugSystemDeviceEnumerator())
        {
            this.DeviceEnumerator.AudioDeviceChanged += DeviceEnumerator_AudioDeviceChanged;
        }

        void DeviceEnumerator_AudioDeviceChanged(object sender, AudioDeviceChangedEventArgs e)
        {
        }
    }
}
// Copyright (c) 2026 Enigma 23. All rights reserved.

namespace e23.TrainController
{
    public class TrackSwitchNext : TrackSwitchUIButton
    {
        protected override void SwitchTrack() 
        {
            foreach (var manager in _trackPathManager)
            {
                manager.NextIndex();
            }
        }
    }
}
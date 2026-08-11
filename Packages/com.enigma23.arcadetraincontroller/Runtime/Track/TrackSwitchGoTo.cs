// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.TrainController
{
    public class TrackSwitchGoTo : TrackSwitchUIButton
    {
        [SerializeField] protected int _goToIndex = 0;

        protected override void SwitchTrack()
        {
            foreach (var manager in _trackPathManager)
            {
                manager.GoTo(_goToIndex);
            }
        }
    }
}
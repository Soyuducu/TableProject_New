// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.Common
{
    [CreateAssetMenu(fileName = "BoostSettings", menuName = "e23/Common/Boost Settings")]
    public class BoostSettings : ScriptableObject
    {
        [Tooltip("Value that the speed will be increased by.")]
        [SerializeField] private float _boostBy = 10f;
        [SerializeField] private float _duration = 2f;
        [Tooltip("How quickly the boost speed is reached. Used for boost up and returning to normal speed.")]
        [SerializeField] private float _timeToBoost = 0.5f;
        public float BoostBy => _boostBy;
        public float Duration => _duration;
        public float TimeToBoost => _timeToBoost;
    }
}
// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.TrainController
{
    [CreateAssetMenu(fileName = nameof(StationSettings), menuName = "e23/ATC/StationSettings")]
    public class StationSettings : ScriptableObject
    {
        [SerializeField] private float _timeToStop = 1f;
        [SerializeField] private float _waitDuration = -1f;
        [SerializeField] private bool _instantStop = false;
        [SerializeField][Range(0.1f, 50f)] private float _resumeSpeed = 10f;
        [SerializeField] private bool _usePreviousSpeed = true;
        [SerializeField] private bool _reverseTrainDirection = false;
        [SerializeField] private bool _ignoreAdditionalCarriages = true;

        public float TimeToStop => _timeToStop;
        public float WaitDuration => _waitDuration;
        public bool InstantStop => _instantStop;
        public float ResumeSpeed => _resumeSpeed;
        public bool UsePreviousSpeed => _usePreviousSpeed;
        public bool ReverseTrainDirection => _reverseTrainDirection;
        public bool IgnoreAdditionalCarriages => _ignoreAdditionalCarriages;
    }
}
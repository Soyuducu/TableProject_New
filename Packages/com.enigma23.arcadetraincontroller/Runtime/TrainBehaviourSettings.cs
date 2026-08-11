// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.TrainController
{
    [CreateAssetMenu(fileName = nameof(TrainBehaviourSettings), menuName = "e23/ATC/TrainBehaviourSettings")]
    public class TrainBehaviourSettings : ScriptableObject
    {
        [Header("Parameters")]
        [SerializeField] private bool _autoStartEngine = true;
        [SerializeField] private bool _autoDrive = false;
        [SerializeField][Range(0.1f, 15f)] private float _throttle = 1f;
        [SerializeField][Range(0.1f, 15f)] private float _acceleration = 1f;
        [SerializeField][Range(0.1f, 15f)] private float _accelerationReverse = 1f;
        [SerializeField][Range(0.1f, 15f)] private float _deceleration = 1f;
        [SerializeField][Range(0.1f, 50f)] private float _maxSpeed = 20f;
        [SerializeField][Range(-50, 0f)] private float _maxSpeedReverse = -15f;
        [SerializeField][Range(0f, 50f)] private float _boostSpeed = 30f;

        public bool AutoStartEngine => _autoStartEngine;
        public bool AutoDrive => _autoDrive;
        public float Throttle => _throttle;
        public float Acceleration => _acceleration;
        public float AccelerationReverse => _accelerationReverse;
        public float Decceleration => _deceleration;
        public float MaxSpeed => _maxSpeed;
        public float MaxSpeedReverse => _maxSpeedReverse;
        public float BoostSpeed => _boostSpeed;
    }
}
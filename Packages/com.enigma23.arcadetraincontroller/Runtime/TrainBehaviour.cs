// Copyright (c) 2026 Enigma 23. All rights reserved.

using e23.TrainController.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace e23.TrainController
{
    [ExecuteAlways]
    public class TrainBehaviour : SplineComponent
    {
        public event Action<bool> OnStartEngine; 
        public event Action<bool> OnEngineStarted;
        public event Action OnStartedMoving;
        public event Action OnStoppedMoving;
        public event Action OnTrackEndReached;
        public event Action OnTrackStartReached;
        public event Action OnLooped;
        public event Action<bool> OnDoorsOpen;

        [SerializeField] protected TrainBehaviourSettings _trainBehaviourSettings;
        [SerializeField] protected TrackPathManager _trackPathManager;
        [Tooltip("Which axis of the GameObject is treated as the forward axis.")]
        [SerializeField] protected AlignAxis _objectForwardAxis = AlignAxis.ZAxis;
        [Tooltip("Which axis of the GameObject is treated as the up axis.")]
        [SerializeField] protected AlignAxis _objectUpAxis = AlignAxis.YAxis;
        [SerializeField][Range(0f, 1f)] protected float _startOffSet = 0f;
        [Tooltip("Set the distance from the pivot to the front of the train, this is used to stop the train before the front goes off the tracks at the end of the spline.")]
        [SerializeField] protected float _halfTrainLength = 0f;
        
        protected float _splinePathLength = -1f;
        protected bool _isFirstNative = false;
        protected bool _pathChangeWaiting = false;
        protected float _currentSpeed = 0f;
        protected float _targetSpeed = 0f;
        protected float _speedMultiplier = 0f;

        protected float _duration = 0f;
        protected float _normalizedTime = 0f;
        protected float _elapsedTime;
        protected float _startOffsetT = 0f;
        protected float _distancePercentage = 0f;
        protected float _distanceTravelled = 0f;
        protected float3 _currentPosition;
        protected bool _isClosedTrack = false;
        protected bool _isBoosting = false;
        protected bool _movingForward = true;

        protected bool _hasLooped = false;
        protected bool _trainHasLeftPathChangeRange = false;
        protected bool _trainInPathChangeRange = false;

        public TrainBehaviourSettings TrainSettings { get => _trainBehaviourSettings; set => _trainBehaviourSettings = value; }
        public float CurrentSpeed => _currentSpeed;
        public float Throttle => _trainBehaviourSettings.Throttle;
        public float Acceleration => _trainBehaviourSettings.Acceleration;
        public float AccelerationReverse => _trainBehaviourSettings.AccelerationReverse;
        public float Deceleration => _trainBehaviourSettings.Decceleration;
        public float MaxSpeed => _trainBehaviourSettings.MaxSpeed;
        public float MaxSpeedReverse => _trainBehaviourSettings.MaxSpeedReverse;
        public float BoostSpeed => _trainBehaviourSettings.BoostSpeed;

        public bool EngineRunning { get; private set; }
        public bool IsBoosting => _isBoosting;
        public bool IsMovingForward => _movingForward;
        public float DistancePercentage => _distancePercentage;

        public TrackPathManager TrackPathManager => _trackPathManager;
        public float StartOffset 
        { 
            get { return _startOffSet; } 
            set 
            { 
                _startOffSet = value;
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    UpdateEditModePosition();
                }
#endif
            }
        }

        /// <summary>
        /// Turn the engine on and off. Use ToggleEngine() for complete engine flow.
        /// </summary>
        /// <param name="enable"></param>
        public virtual void InvokeOnStartEngine(bool enable) => OnStartEngine?.Invoke(enable);

        /// <summary>
        /// Call this once the engine has started, for example after any audio SFX for the engine turning on/ off.
        /// </summary>
        /// <param name="enable"></param>
        public virtual void InvokeEngineStarted(bool enable)
        {
            EngineRunning = enable;
            OnEngineStarted?.Invoke(enable);
        }

        public virtual void InvokeStartedMoving() => OnStartedMoving?.Invoke();
        public virtual void InvokeStoppedMoving() => OnStoppedMoving?.Invoke();

        /// <summary>
        /// Notification for train making a complete loop around the track.
        /// </summary>
        public virtual void InvokeOnLooped()
        {
            _hasLooped = false;
            OnLooped?.Invoke();
        }
        protected virtual void InvokeStartReached() => OnTrackStartReached?.Invoke();
        protected virtual void InvokeEndReached() => OnTrackEndReached?.Invoke();
        protected virtual void InvokeDoors(bool opening) => OnDoorsOpen?.Invoke(opening);

        protected virtual void OnValidate()
        {
            if (Application.isPlaying) { return; }

            UpdateEditModePosition();
        }
        
        protected virtual void Awake() { }
        protected virtual void OnEnable() => RegisterActions(true);
        protected virtual void OnDisable() => RegisterActions(false);
        
        protected virtual void Start()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false) { return; }
#endif
            GetPathInfo();
            SetStartoffset();
            GetRequiredComponents();
            AutoDrive();
        }

        protected virtual void GetRequiredComponents()
        {
            if (_trainBehaviourSettings == null)
            {
                Debug.LogWarning($"TrainBehaviourSettings has not been assigned to {gameObject.name}.\nIgnore this warning when displayed after building using the prefab builder.", gameObject); 
                return;
            }

            bool invokeEngine = TryGetComponent(out TrainAudio _);
            ToggleEngine(_trainBehaviourSettings.AutoStartEngine, !invokeEngine);
        }

        protected virtual void GetPathInfo()
        {
            if (TrackPathManager.SplineContainer == null)
            {
                Debug.LogError($"SplineContainer not assigned on {gameObject.name}", gameObject);
                return;
            }

            _splinePathLength = TrackPathManager.PathLength;
            _isClosedTrack = TrackPathManager.Closed;
        }

        protected virtual void RegisterActions(bool register)
        {
            if (TrackPathManager == null) { return; }

            TrackPathManager.OnPathRebuilt -= PathChanged;

            if (register == false) { return; }

            TrackPathManager.OnPathRebuilt += PathChanged;
        }

        protected virtual void AutoDrive()
        {
            if (_trainBehaviourSettings.AutoDrive == false) { return; }

            float speed = IsMovingForward == true ? _trainBehaviourSettings.MaxSpeed : _trainBehaviourSettings.MaxSpeedReverse;
            StartMoving(speed);
        }

#if UNITY_EDITOR
        protected virtual void UpdateEditModePosition()
        {
            if (TrackPathManager == null || TrackPathManager.SplineContainer == null) { return; }
            if (TrackPathManager.SplineContainer.Splines == null || TrackPathManager.SplineContainer.Splines.Count == 0) { return; }

            NativeSpline tempSpline = default;
            bool tempCreated = false;

            try
            {
                SplinePath splinePath;

                if (TrackPathManager.Switches == null || TrackPathManager.Switches.Count == 0)
                {
                    _isClosedTrack = TrackPathManager.SplineContainer.Splines.First().Closed;

                    var slices = new List<SplineSlice<Spline>>();
                    if (_isClosedTrack == false)
                    {
                        slices.Add(new SplineSlice<Spline>
                        (
                            TrackPathManager.SplineContainer.Splines.First(),
                            new SplineRange(0, TrackPathManager.SplineContainer.Splines[0].Count)
                        ));
                    }
                    else
                    {
                        slices.Add(new SplineSlice<Spline>
                        (
                            TrackPathManager.SplineContainer.Splines.First(),
                            new SplineRange(0, TrackPathManager.SplineContainer.Splines[0].Knots.Count() + 1)
                        ));
                    }

                    splinePath = new SplinePath(slices);
                }
                else
                {
                    var slices = new List<SplineSlice<Spline>>();

                    for (int i = 0; i < TrackPathManager.Switches.Count; i++)
                    {
                        var splineRangeData = TrackPathManager.Switches[i].SplineRangeData(TrackPathManager.CurrentPathIndex);
                        if (splineRangeData.Ignore) { continue; }

                        Spline spline = TrackPathManager.SplineContainer.Splines[splineRangeData.Spline];

                        var slice = new SplineSlice<Spline>
                        (
                            spline,
                            new SplineRange(splineRangeData.StartKnot, splineRangeData.knotCount)
                        );

                        slices.Add(slice);
                    }

                    splinePath = new SplinePath(slices);
                }

                tempSpline = new NativeSpline(splinePath, Allocator.TempJob);
                tempCreated = true;

                float t = _startOffSet;

                SplineUtility.Evaluate
                (
                    tempSpline, t,
                    out float3 localPos,
                    out float3 localTangent,
                    out float3 localUp
                );

                transform.position = TrackPathManager.SplineContainer.transform.TransformPoint(localPos);

                var forward = (Vector3) TrackPathManager.SplineContainer.transform.TransformDirection(localTangent);
                var up = (Vector3) TrackPathManager.SplineContainer.transform.TransformDirection(localUp);

                if (forward.magnitude > Mathf.Epsilon)
                {
                    var remappedForward = GetAxis(_objectForwardAxis);
                    var remappedUp = GetAxis(_objectUpAxis);
                    var axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(remappedForward, remappedUp));
                    transform.rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;
                }
            }
            finally
            {
                if (tempCreated == true) { tempSpline.Dispose(); }
            }

        }
#endif
        protected virtual void SetStartoffset() 
        {
            _distanceTravelled = _startOffSet * _splinePathLength;
            _distancePercentage = _startOffSet;

            EvaluatePositionAndRotation(_distancePercentage, out var position, out var rotation);

            transform.position = position;
            transform.rotation = rotation;
        }

        protected virtual void FixedUpdate()
        {
            if (_pathChangeWaiting) { CheckLocationOnPathForChange(); }

            _distanceTravelled += _currentSpeed * Time.deltaTime;
    
            if (_isClosedTrack) 
            { 
                _distanceTravelled %= _splinePathLength;
                if (_distanceTravelled < 0f) { _distanceTravelled += _splinePathLength; }
            }
            else { _distanceTravelled = Mathf.Clamp(_distanceTravelled, 0f, _splinePathLength); }

            _distancePercentage = _distanceTravelled / _splinePathLength;

            DistancePercentageCheck();
            CalculateNormalizedTime(Time.deltaTime);
            Accelerate();
            EvaluatePositionAndRotation(_distancePercentage, out var position, out var rotation);

            transform.position = position;
            transform.rotation = rotation;

            InvokeEndReached();
            InvokeStartReached();
        }

        protected virtual void CheckLocationOnPathForChange()
        {
            if (TrackPathManager.IsInPathChangeRange(_distancePercentage))
            {
                GetNewPathTravelled(TrackPathManager.BuiltSplinePending);

                _pathChangeWaiting = false;
                TrackPathManager.ChangePath();

                InvokeOnLooped();
                GetPathInfo();
            }
        }

        protected virtual void DistancePercentageCheck()
        {
            if (_isClosedTrack == true)
            {
                if (_distancePercentage > 1f) { _distancePercentage -= 1f; _hasLooped = true; }
                else if (_distancePercentage < 0) { _distancePercentage += 1f; _hasLooped = true; }
            }
            else
            {
                if ((_distancePercentage > (1f - _halfTrainLength) && _movingForward == true) || (_distancePercentage < _halfTrainLength && _movingForward == false))
                {
                    StopInstantly();
                }
            }
        }

        protected virtual void CalculateNormalizedTime(float deltaTime)
        {
            _elapsedTime += deltaTime;
            float currentDuration = _duration;

            var t = _elapsedTime % currentDuration;
            t /= currentDuration;
            _normalizedTime = t == 0 ? 0f : Mathf.Floor(_normalizedTime) + t;
        }

        public virtual void ResetPathChangeRangeStatus()
        {
            _trainHasLeftPathChangeRange = false;
            _trainInPathChangeRange = false;
        }

        protected virtual void Accelerate() => _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * _speedMultiplier);

        protected virtual void EvaluatePositionAndRotation(float distancePercentage, out Vector3 position, out Quaternion rotation)
        {
            var t = GetLoopInterpolation(true);

            SplineUtility.Evaluate
            (
                TrackPathManager.BuiltSpline,
                distancePercentage,
                out float3 worldPos,
                out float3 worldTangent,
                out float3 worldUp);

            position = worldPos;
            _currentPosition = worldPos;

            var forward = (Vector3) worldTangent;
            var up = (Vector3) worldUp;

            if (forward.magnitude <= Mathf.Epsilon)
            {
                float fallbackT = t < 1f ? Mathf.Min(1f, t + 0.01f) : t - 0.01f;

                forward = (Vector3) SplineUtility.EvaluateTangent(TrackPathManager.BuiltSpline, fallbackT);
            }

            forward.Normalize();

            var remappedForward = GetAxis(_objectForwardAxis);
            var remappedUp = GetAxis(_objectUpAxis);
            var axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(remappedForward, remappedUp));

            rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;
        }

        protected virtual float GetLoopInterpolation(bool offset)
        {
            float t;
            var normalizedTimeWithOffset = _normalizedTime + (offset ? _startOffsetT : 0f);
            if (Mathf.Floor(normalizedTimeWithOffset) == normalizedTimeWithOffset)
            { t = Mathf.Clamp01(normalizedTimeWithOffset); }
            else
            { t = normalizedTimeWithOffset % 1f; }
            
            return t;
        }

        protected virtual void PathChanged(NativeSpline nativeSpline) => _pathChangeWaiting = true;

        protected virtual void GetNewPathTravelled(NativeSpline nativeSpline)
        {
            SplineUtility.GetNearestPoint
            (
                nativeSpline,
                _currentPosition,
                out float3 nearest,
                out float nearestT
            );

            var newPathLength = nativeSpline.GetLength();
            _distanceTravelled = nearestT * newPathLength;
            _splinePathLength = newPathLength;
        }

        protected virtual void CalculateDuration()
        {
            if (_splinePathLength >= 0f) { _duration = _splinePathLength / _currentSpeed; }
        }

        /// <summary>
        /// Toggle the engine on/ off by passing in 'true' or 'false'.
        /// </summary>
        /// <param name="enableEngine"></param>
        /// <param name="invokeEngineStarted"></param>
        public virtual void ToggleEngine(bool enableEngine, bool invokeEngineStarted)
        {
            InvokeOnStartEngine(enableEngine);
            
            if (invokeEngineStarted) { InvokeEngineStarted(enableEngine); }
        }

        /// <summary>
        /// Increase the throttle of the train. Throttle is set in the assigned TrainBehaviourSettings.
        /// </summary>
        public virtual void IncreaseThrottle()
        {
            if (EngineRunning == false) { return; }

            if (_targetSpeed > MaxSpeed) { return; }
            
            if (_currentSpeed == 0) { InvokeStartedMoving(); }

            _speedMultiplier = Acceleration;
            _movingForward = true;
            _targetSpeed += Throttle;
            CalculateDuration();
        }

        /// <summary>
        /// Decrease the throttle of the train. Throttle is set in the assigned TrainBehaviourSettings.
        /// </summary>
        public virtual void DecreaseThrottle()
        {
            if (EngineRunning == false) { return; }

            if (_targetSpeed < MaxSpeedReverse) { return; }

            _speedMultiplier = AccelerationReverse;
            _movingForward = false;
            _targetSpeed -= Throttle;
            CalculateDuration();

            if (_currentSpeed == 0) { InvokeStoppedMoving(); }
        }

        /// <summary>
        /// Start the train moving to the speed passed in.
        /// Example: Stations setting the train moving again.
        /// </summary>
        /// <param name="speed"></param>
        public virtual void StartMoving(float speed)
        {
            _speedMultiplier = Acceleration;
            _targetSpeed = speed;
            
            InvokeStartedMoving();
        }

        /// <summary>
        /// Stop the train in passed in time.
        /// </summary>
        /// <param name="stopTime"></param>
        public virtual void StopMoving(float stopTime)
        {
            _speedMultiplier = Deceleration;
            StartCoroutine(SlowToStop(stopTime));
        }

        public virtual void StopInstantly()
        {
            _targetSpeed = 0f;
            _currentSpeed = Mathf.Lerp(_targetSpeed, _currentSpeed, Time.deltaTime * Acceleration);

            InvokeStoppedMoving();
        }

        /// <summary>
        /// Set the train moving backwards.
        /// </summary>
        public virtual void ReverseDirection()
        {
            float speed;
            if (_movingForward == true)
            {
                _movingForward = false;
                speed = _trainBehaviourSettings.MaxSpeedReverse;
            }
            else
            {
                _movingForward = true;
                speed = _trainBehaviourSettings.MaxSpeed;
            }
            
            StartMoving(speed);
        }

        protected virtual IEnumerator SlowToStop(float duration)
        {
            float timeElapsed = 0;

            while (timeElapsed < duration)
            {
                _targetSpeed = Mathf.Lerp(_currentSpeed, 0f, timeElapsed / duration);
                timeElapsed += Time.deltaTime;

                yield return null;
            }

            _targetSpeed = 0f;
            InvokeStoppedMoving();
        }

        /// <summary>
        /// Sets isBoosting to true. Set your boost speed in the TrainBehaviourSettings asset.
        /// </summary>
        public virtual void Boost() => _isBoosting = true;
        /// <summary>
        /// Sets isBoosting to false.
        /// </summary>
        public virtual void StopBoost() => _isBoosting = false;

        /// <summary>
        /// Performs a timed boost, pass in a float for how long the boost should last in seconds.
        /// </summary>
        /// <param name="boostLength">Duration in seconds.</param>
        public virtual void OneShotBoost(float boostLength)
        {
            if (_isBoosting == false)
            {
                StartCoroutine(BoostTimer(boostLength));
            }
        }

        protected IEnumerator BoostTimer(float boostLength)
        {
            Boost();

            yield return new WaitForSeconds(boostLength);

            StopBoost();
        }

        /// <summary>
        /// Performs a timed boost with a customisable speed. 
        /// </summary>
        /// <param name="boostSpeed">Value to increase speed by.</param>
        /// <param name="boostLength">Duration in seconds.</param>
        /// <param name="timeToBoost">Duration in seconds to reach boost speed.</param>
        public virtual void OneShotBoost(float boostSpeed, float boostLength, float timeToBoost) => StartCoroutine(BoostCustomSpeed(boostSpeed, boostLength, timeToBoost));

        protected virtual IEnumerator BoostCustomSpeed(float boostSpeed, float boostLength, float timeToBoost)
        {
            float timeElapsed = 0;
            float startSpeed = _currentSpeed;
            float targetSpeed = _targetSpeed + boostSpeed;
            
            while (timeElapsed < timeToBoost)
            {
                _targetSpeed = Mathf.Lerp(startSpeed, targetSpeed, timeElapsed / timeToBoost);
                timeElapsed += Time.deltaTime;
            }

            yield return new WaitForSeconds(boostLength);

            timeElapsed = 0;
            startSpeed = _currentSpeed;
            targetSpeed = _currentSpeed - boostSpeed;
            
            while (timeElapsed < timeToBoost)
            {
                _targetSpeed = Mathf.Lerp(startSpeed, targetSpeed, timeElapsed / timeToBoost);
                timeElapsed += Time.deltaTime;
            }
        }

        public virtual void OpenDoors() => InvokeDoors(true);
        public virtual void CloseDoors() => InvokeDoors(false);
    }
}
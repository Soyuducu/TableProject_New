// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace e23.TrainController
{
    [ExecuteAlways]
    public class MultiTrainBehaviour : TrainBehaviour
    {
        [SerializeField] protected List<Carriage> _carriages;
        [SerializeField][Range(0f, 10f)] protected float _carriageDistance = 0.1f;

        [SerializeField] protected List<float> _carriageDistances = new List<float>();

        public List<Carriage> Carriages { get => _carriages; set => _carriages = value; }
        public int CarriageCount => _carriages.Count - 1;
        
        protected override void OnValidate()
        {
            if (Application.isPlaying) { SetStartoffset(); }
            else { UpdateEditModePosition(); }
        }

        protected override void Awake()
        {
            base.Awake();
            GetRequiredComponents();
        }

        protected override void Start() 
        {
            if (Application.isPlaying == false) { return; }
            GetPathInfo();
            GetRequiredComponents();
            SetStartoffset();
            AutoDrive();
        }
        
        protected override void GetRequiredComponents()
        {
            base.GetRequiredComponents();

            if (_carriages == null) 
            { 
                _carriages = new List<Carriage>(); 
                _carriages.AddRange(GetComponentsInChildren<Carriage>());
            }
        }

#if UNITY_EDITOR
        protected override void UpdateEditModePosition()
        {
            if (_trackPathManager == null || TrackPathManager.SplineContainer == null) { return; }
            if (TrackPathManager.SplineContainer.Splines == null || TrackPathManager.SplineContainer.Splines.Count == 0) { return; }
            if (_carriages == null || _carriages.Count == 0) { return; }

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

                float splineLength = tempSpline.GetLength();
                float currentDistance = _startOffSet * splineLength;

                for (int i = 0; i < _carriages.Count; i++)
                {
                    if (_carriages[i] == null) { continue; }

                    float t = splineLength > 0f ? (currentDistance % splineLength) / splineLength : 0f;

                    SplineUtility.Evaluate
                    (
                        tempSpline, t,
                        out float3 localPos,
                        out float3 localTangent,
                        out float3 localUp
                    );

                    _carriages[i].transform.position = TrackPathManager.SplineContainer.transform.TransformPoint(localPos);

                    var forward = (Vector3) TrackPathManager.SplineContainer.transform.TransformDirection(localTangent);
                    var up = (Vector3) TrackPathManager.SplineContainer.transform.TransformDirection(localUp);

                    if (forward.magnitude > Mathf.Epsilon)
                    {
                        var remappedForward = GetAxis(_objectForwardAxis);
                        var remappedUp = GetAxis(_objectUpAxis);
                        var axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(remappedForward, remappedUp));
                        _carriages[i].transform.rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;
                    }

                    currentDistance -= _carriageDistance;

                    while (currentDistance < 0f) { currentDistance += splineLength; }
                    while (currentDistance > splineLength) { currentDistance -= splineLength; }
                }
            }
            finally
            {
                if (tempCreated == true) { tempSpline.Dispose(); }
            }
        }
#endif

        protected override void SetStartoffset()
        {
            if (_carriages == null || _carriages.Count == 0) { GetRequiredComponents(); }

            _distanceTravelled = _startOffSet * _splinePathLength;
            _distancePercentage = _startOffSet;

            if (_carriageDistances.Count > 0) { _carriageDistances.Clear(); }

            float currentDistance = _startOffSet * _splinePathLength;
            foreach (Carriage carriage in _carriages)
            {
                _carriageDistances.Add(currentDistance);
                currentDistance -= _carriageDistance;

                if (currentDistance < 0f)
                {
                    if (_isClosedTrack) { currentDistance += _splinePathLength; }
                    else { currentDistance = 0f; }
                }
            }
            
            for (int i = 0; i < _carriages.Count; i++)
            {
                float t = _carriageDistances[i] / _splinePathLength;
                EvaluatePositionAndRotation(t, out var position, out var rotation);

                _carriages[i].transform.position = position;
                _carriages[i].transform.rotation = rotation;
            }
        }

        protected override void FixedUpdate()
        {
            if (_pathChangeWaiting) { CheckLocationOnPathForChange(); }
            
            _distanceTravelled += _currentSpeed * Time.deltaTime;
    
            if (_isClosedTrack) { _distanceTravelled %= _splinePathLength; }
            else { _distanceTravelled = Mathf.Clamp(_distanceTravelled, 0f, _splinePathLength); }

            _distancePercentage = _distanceTravelled / _splinePathLength;

            DistancePercentageCheck();
            CalculateNormalizedTime(Time.deltaTime);
            Accelerate();

            for (int i = 0; i < _carriages.Count; i++)
            {
                Transform carriageTransform = _carriages[i].transform;
                _carriageDistances[i] += _currentSpeed * Time.deltaTime;

                if (_isClosedTrack) { _carriageDistances[i] %= _splinePathLength; }
                else { _carriageDistances[i] = Mathf.Clamp(_carriageDistances[i], 0f, _splinePathLength); }

                float t = _carriageDistances[i] / _splinePathLength;
                EvaluatePositionAndRotation(t, out var position, out var rotation);

                carriageTransform.position = position;
                carriageTransform.rotation = rotation;
            }

            InvokeEndReached();
            InvokeStartReached();
        }

        protected override void CheckLocationOnPathForChange()
        {
            if (TrackPathManager.InstantPathChange)
            {
                base.CheckLocationOnPathForChange();
                TrackPathManager.SetInstantPathChange(false);
                return;
            }

            float lastCarriageDistancePercentage = _carriageDistances[CarriageCount] / _splinePathLength;
            bool trainCurrentlyInRange = TrackPathManager.IsInPathChangeRange(_distancePercentage);

            if (trainCurrentlyInRange == false)
            {
                _trainHasLeftPathChangeRange = true;
                _trainInPathChangeRange = false;
            }
            else if (trainCurrentlyInRange == true && _trainHasLeftPathChangeRange == true)
            {
                float distanceToStart = Mathf.Abs(_distancePercentage - TrackPathManager.PathChangeRangeStart);
                float distanceToEnd = Mathf.Abs(_distancePercentage - TrackPathManager.PathChangeRangeEnd);
                bool enteredFromStart = distanceToStart < distanceToEnd;

                if (_movingForward && !enteredFromStart) { _trainInPathChangeRange = true; }
                else if (_movingForward == false && enteredFromStart) { _trainInPathChangeRange = true; }
            }

            if (_trainInPathChangeRange && TrackPathManager.IsInPathChangeRange(lastCarriageDistancePercentage))
            { base.CheckLocationOnPathForChange(); }
        }

        protected override void DistancePercentageCheck()
        {
            base.DistancePercentageCheck();
            
            if (_isClosedTrack == true)
            {
                for (int i = 0; i < _carriageDistances.Count; i++)
                {
                    if (_carriageDistances[i] > _splinePathLength) { _carriageDistances[i] = 0f; }
                    else if (_carriageDistances[i] < 0f) { _carriageDistances[i] = _splinePathLength; }
                }
            }
        }

        protected override void GetNewPathTravelled(NativeSpline nativeSpline)
        {
            base.GetNewPathTravelled(nativeSpline);

            SplineUtility.GetNearestPoint
            (
                nativeSpline,
                (float3)_carriages[0].transform.position,
                out float3 nearest,
                out float nearestT
            );

            _carriageDistances[0] = nearestT * _splinePathLength;

            for (int i = 1; i < _carriageDistances.Count; i++)
            {
                _carriageDistances[i] = _carriageDistances[i - 1] - _carriageDistance;

                if (_carriageDistances[i] < 0f)
                {
                    if (_isClosedTrack) { _carriageDistances[i] += _splinePathLength; }
                    else { _carriageDistances[i] = 0f; }
                }
            }
        }
    }
}
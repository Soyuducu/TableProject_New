// Copyright (c) 2026 Enigma 23. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace e23.TrainController
{
    public class TrackPathManager : MonoBehaviour
    {
        public event Action<NativeSpline> OnPathRebuilt;

        [SerializeField] protected SplineContainer _splineContainer;
        [SerializeField] protected List<TrackSwitch> _switches;

        [SerializeField] protected int _currentPathIndex = 0;
        [SerializeField] protected int _previousPathIndex = 0;
        [SerializeField] protected float _splinePathLength;
        [SerializeField] protected bool _isClosedTrack = false;
        [SerializeField] protected float _knotDistance = 0f;
        [SerializeField] protected float _knotDistanceNormalised = 0f;
        [SerializeField] protected float _pathChangeRangeStart = 0f;
        [SerializeField] protected float _pathChangeRangeEnd = 0f;

        protected NativeSpline _activeNativeSpline;
        protected NativeSpline _firstNativeSpline;
        protected NativeSpline _secondNativeSpline;
        protected bool _pathCreated = false;
        protected bool _isFirstPending = false;
        protected bool _firstNativeSplineCreated = false;
        protected bool _secondNativeSplineCreated = false;
        protected bool _instantPathChange = false;

        public SplineContainer SplineContainer { get => _splineContainer; set => _splineContainer = value; }
        public int CurrentPathIndex => _currentPathIndex;
        public List<TrackSwitch> Switches => _switches;
        public float PathLength => _splinePathLength;
        public bool Closed => _isClosedTrack;
        public float KnotDistance => _knotDistance;
        public float KnotDistanceNormalised => _knotDistanceNormalised;
        public float PathChangeRangeStart => _pathChangeRangeStart;
        public float PathChangeRangeEnd => _pathChangeRangeEnd;
        public bool PathChangeRangeWraps => _pathChangeRangeStart > _pathChangeRangeEnd;
        public bool InstantPathChange => _instantPathChange;

        public NativeSpline BuiltSpline => _activeNativeSpline;
        public NativeSpline BuiltSplinePending
        {
            get
            {
                if (_isFirstPending) { return _firstNativeSpline; }
                else { return _secondNativeSpline; }
            }
        }

        public virtual void AddSwitch(TrackSwitch trackSwitch)
        {
            if (_switches == null) { _switches = new List<TrackSwitch>(); }
            _switches.Add(trackSwitch);
        }

        public virtual void AssignSplineContainer(SplineContainer splineContainer) => _splineContainer = splineContainer;

        protected virtual void Awake()
        {
            GetRequiredComponents();
            RebuildPath();
        }

        protected virtual void OnDestroy() => Dispose();

        protected virtual void GetRequiredComponents()
        {
            if (_splineContainer == null) { _splineContainer = GetComponentInParent<SplineContainer>(); }
            if (_switches.Count == 0) { _switches = new List<TrackSwitch>(GetComponentsInChildren<TrackSwitch>()); }
        }

        public virtual void NextIndex()
        {
            _previousPathIndex = _currentPathIndex;
            _currentPathIndex = (_currentPathIndex < _switches[0].PathCountMinusOne) ? _currentPathIndex + 1 : 0;

            BuildSplineDataRange();
        }

        public virtual void PreviousIndex()
        {
            _previousPathIndex = _currentPathIndex;
            _currentPathIndex = (_currentPathIndex - 1) >= 0 ? _currentPathIndex - 1 : _switches[0].PathCountMinusOne;

            BuildSplineDataRange();
        }

        public virtual void GoTo(int index)
        {
            if (index >= _switches.Count || _currentPathIndex == index) { return; }

            _currentPathIndex = index;
            BuildSplineDataRange();
        }

        protected virtual void BuildSplineDataRange()
        {
            RebuildPath();

            SplineRangeData sharedRangeData = null;
            for (int i = 0; i < _switches.Count; i++)
            {
                SplineRangeData prev = _switches[i].SplineRangeData(_previousPathIndex);
                SplineRangeData next = _switches[i].SplineRangeData(_currentPathIndex);

                if (next.Ignore && prev.Ignore) { continue; }

                bool splineDiffers = prev.Spline != next.Spline;
                bool startKnotDiffers = prev.StartKnot != next.StartKnot;
                bool knotCountDiffers = prev.knotCount != next.knotCount;

                if (splineDiffers || startKnotDiffers || knotCountDiffers)
                {
                    if (!next.Ignore && next.IsJunction)
                    {
                        sharedRangeData = next;
                        break;
                    }
                    else if (!prev.Ignore && prev.IsJunction)
                    {
                        sharedRangeData = prev;
                        break;
                    }
                }
            }

            if (sharedRangeData == null)
            {
                Debug.LogWarning("TrackPathManager.NextIndex: could not find a diverging switch with IsJunction set; path change range not updated.", gameObject);
                return;
            }

            int divergeKnot = sharedRangeData.StartKnot;
            int rejoinKnot = sharedRangeData.StartKnot + sharedRangeData.knotCount - 1;

            CalculatePathChangeRange(sharedRangeData.Spline, divergeKnot, rejoinKnot);

#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }
        public virtual void RebuildPath()
        {
            if (_splineContainer == null) { Debug.LogError($"SplineContainer has not been assigned, track cannot be built.", gameObject); return; }

            SplinePath splinePath;

            if (_switches == null || _switches.Count == 0)
            {
                _isClosedTrack = _splineContainer.Splines.First().Closed;

                var slices = new List<SplineSlice<Spline>>();
                if (_isClosedTrack == false)
                {
                    slices.Add(new SplineSlice<Spline>
                    (
                        _splineContainer.Splines.First(),
                        new SplineRange(0, _splineContainer.Splines[0].Count)
                    ));
                }
                else
                {
                    slices.Add(new SplineSlice<Spline>
                    (
                        _splineContainer.Splines.First(),
                        new SplineRange(0, _splineContainer.Splines[0].Knots.Count() + 1)
                    ));
                }

                splinePath = new SplinePath(slices);
            }
            else
            {
                _isClosedTrack = false;

                var slices = new List<SplineSlice<Spline>>();

                for (int i = 0; i < _switches.Count; i++)
                {
                    var splineRangeData = _switches[i].SplineRangeData(_currentPathIndex);
                    if (splineRangeData.Ignore) { continue; }

                    Spline spline = _splineContainer.Splines[splineRangeData.Spline];

                    var slice = new SplineSlice<Spline>
                    (
                        spline,
                        new SplineRange(splineRangeData.StartKnot, splineRangeData.knotCount)
                    );

                    slices.Add(slice);

                    _isClosedTrack = splineRangeData.Closed;
                }

                splinePath = new SplinePath(slices);
            }

            if (_firstNativeSplineCreated == false) { CreateFirst(); }
            else { CreateSecond(); }

            if (_switches != null && _switches.Count > 0 && _pathCreated == false)
            {
                _activeNativeSpline = _firstNativeSpline;
                _splinePathLength = _activeNativeSpline.GetLength();
                _pathCreated = true;
                _isFirstPending = false;

                for (int i = 0; i < _switches.Count; i++)
                {
                    SplineRangeData data = _switches[i].SplineRangeData(_currentPathIndex);
                    if (data.Ignore || !data.IsJunction) { continue; }

                    int divergeKnot = data.StartKnot;
                    int rejoinKnot = data.StartKnot + data.knotCount - 1;
                    CalculatePathChangeRange(data.Spline, divergeKnot, rejoinKnot);
                    break;
                }

                return;
            }
            else if (_pathCreated == false)
            {
                _activeNativeSpline = _firstNativeSpline;
                _splinePathLength = _activeNativeSpline.GetLength();
                _pathCreated = true;
                _isFirstPending = false;

                return;
            }

            OnPathRebuilt?.Invoke(BuiltSplinePending);

            void CreateFirst()
            {
                _firstNativeSpline = new NativeSpline(splinePath, Allocator.Persistent);
                _isFirstPending = true;
                _firstNativeSplineCreated = true;
            }

            void CreateSecond()
            {
                _secondNativeSpline = new NativeSpline(splinePath, Allocator.Persistent);
                _isFirstPending = false;
                _secondNativeSplineCreated = true;
            }
        }

        /// <summary>
        /// Returns true when normalisedDistance falls within the safe range to change paths.
        /// Handles both wrapping (e.g. [0,9] to [0,1]) and non-wrapping ranges.
        /// </summary>
        public bool IsInPathChangeRange(float normalisedDistance)
        {
            if (PathChangeRangeWraps) { return normalisedDistance >= _pathChangeRangeStart || normalisedDistance <= _pathChangeRangeEnd; }
            else { return normalisedDistance >= _pathChangeRangeStart && normalisedDistance <= _pathChangeRangeEnd; }
        }

        /// <summary>
        /// Allows the path to instantly change.
        /// Example: Train has reached the end of a track with no loop.
        /// </summary>
        /// <param name="instant"></param>
        public virtual void SetInstantPathChange(bool instant) => _instantPathChange = instant;

        /// <summary>
        /// Calculates the normalised range [start, end] on the active spline within which changing path is safe.
        /// Knot positions are projected onto the active spline so that t values are consistent with the train's normalised distance percentage.
        /// </summary>
        public virtual void CalculatePathChangeRange(int splineIndex, int divergeKnot, int rejoinKnot)
        {
            int knotCount = _splineContainer.Splines[splineIndex].Knots.Count();
            divergeKnot = Mathf.Clamp(divergeKnot, 0, knotCount - 1);
            rejoinKnot = Mathf.Clamp(rejoinKnot, 0, knotCount - 1);

            NativeSpline activePath = _activeNativeSpline;
            float3 divergePos = _splineContainer.Splines[splineIndex][divergeKnot].Position;
            float3 rejoinPos = _splineContainer.Splines[splineIndex][rejoinKnot].Position;

            SplineUtility.GetNearestPoint(activePath, divergePos, out _, out float tDiverge);
            SplineUtility.GetNearestPoint(activePath, rejoinPos, out _, out float tRejoin);

            _pathChangeRangeStart = Mathf.Min(tDiverge, tRejoin);
            _pathChangeRangeEnd = Mathf.Max(tDiverge, tRejoin);

            _knotDistanceNormalised = _pathChangeRangeStart;
            _knotDistance = SplineUtility.ConvertIndexUnit
            (
                activePath,
                _pathChangeRangeStart,
                PathIndexUnit.Normalized,
                PathIndexUnit.Distance
            );
        }

        /// <summary>
        /// Call this method when it's safe for the train to change path.
        /// </summary>
        public virtual void ChangePath()
        {
            if (_isFirstPending == true)
            {
                _isFirstPending = false;
                _activeNativeSpline = _firstNativeSpline;

                _secondNativeSpline.Dispose();
                _secondNativeSplineCreated = false;
            }
            else
            {
                _activeNativeSpline = _secondNativeSpline;

                _firstNativeSpline.Dispose();
                _firstNativeSplineCreated = false;
            }

            _splinePathLength = _activeNativeSpline.GetLength();
        }

        protected virtual void Dispose()
        {
            if (_pathCreated == false) { return; }

            _isFirstPending = false;

            if (_firstNativeSplineCreated == true)
            {
                _firstNativeSpline.Dispose();
                _firstNativeSplineCreated = false;
            }

            if (_secondNativeSplineCreated == true)
            {
                _secondNativeSpline.Dispose();
                _secondNativeSplineCreated = false;
            }
        }
    }
}
// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace e23.TrainController
{
    public class TrackSwitch : MonoBehaviour
    {
        [SerializeField] protected List<SplineRangeData> _splineKnotIndices;

        public int PathCount => _splineKnotIndices.Count;
        public int PathCountMinusOne => _splineKnotIndices.Count - 1;
        public SplineRangeData SplineRangeData(int index) => _splineKnotIndices[index];

        public virtual void CreateSplineRangeData(bool ignore, int spline, int startKnot, int knotCount, bool closed, bool isJunction)
        {
            if (_splineKnotIndices == null) { _splineKnotIndices  = new List<SplineRangeData>(); }

            SplineRangeData newSplineRangeData = new SplineRangeData();
            newSplineRangeData.Ignore = ignore;
            newSplineRangeData.Spline = spline;
            newSplineRangeData.StartKnot = startKnot;
            newSplineRangeData.knotCount = knotCount;
            newSplineRangeData.Closed = closed;
            newSplineRangeData.IsJunction = isJunction;

            _splineKnotIndices.Add(newSplineRangeData);
        }
    }

    [System.Serializable]
    public class SplineRangeData
    {
        public bool Ignore = false;
        public int Spline;
        public int StartKnot;
        public int knotCount;
        public bool Closed;
        public bool IsJunction = false;
    }
}
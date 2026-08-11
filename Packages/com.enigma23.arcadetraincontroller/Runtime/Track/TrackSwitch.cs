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
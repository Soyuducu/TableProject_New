// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace e23.TrainController
{
    public class TrackSwitchVisual : MonoBehaviour
    {
        [SerializeField] protected TrackPathManager _trackPathManager;
        [SerializeField] protected List<IndexAngles> _trackAngles;

        public TrackPathManager TrackPathManager { get => _trackPathManager; set => _trackPathManager = value; }

        public virtual void AddAngles(int pathIndex, float angle)
        {
            if (_trackAngles == null) { _trackAngles = new List<IndexAngles>(); }

            IndexAngles newIndexAngles = new IndexAngles();
            newIndexAngles.pathIndex = pathIndex;
            newIndexAngles.angle = angle;

            _trackAngles.Add(newIndexAngles);
        }

        protected virtual void Awake() => UpdateAlignment();
        protected virtual void OnEnable() => RegisterActions(true);
        protected virtual void OnDisable() => RegisterActions(false);

        protected virtual void RegisterActions(bool register)
        {
            _trackPathManager.OnPathRebuilt -= UpdateAlignment;
            
            if (register == false) { return; }

            _trackPathManager.OnPathRebuilt += UpdateAlignment;
        }

        protected virtual void UpdateAlignment(NativeSpline nativeSpline) => UpdateAlignment();

        protected virtual void UpdateAlignment()
        {
            int pathIndex = _trackPathManager.CurrentPathIndex;
            Vector3 newAngle = new Vector3(0f, _trackAngles[pathIndex].angle, 0f);

            transform.eulerAngles = newAngle;
        }
    }

    [System.Serializable]
    public class IndexAngles
    {
        public int pathIndex = 0;
        public float angle = 0f;
    }
}
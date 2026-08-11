// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace e23.TrainController
{
    public class Wheels : MonoBehaviour
    {
#pragma warning disable 0649
        [Tooltip("If left empty an attempt to find a TrainBehaviour on this GameObject is made.")]
        [SerializeField] protected TrainBehaviour _trainBehaviour;
        [SerializeField] protected List<Transform> _trainWheels;
#pragma warning restore 0649
        protected List<float> _wheelRadii;

        public TrainBehaviour TrainBehavour { get => _trainBehaviour; set => _trainBehaviour = value; }
        public List<Transform> TrainWheels { get => _trainWheels; set => _trainWheels = value; }

        protected virtual void Awake()
        {
            if (_trainBehaviour == null) { GetRequiredComponents(); }
            if (_trainWheels.Count == 0) { Debug.LogWarning($"No wheels have been assigned.", gameObject); return; }

            GetWheelRadius();
        }

        protected virtual void GetRequiredComponents()
        {
            _trainBehaviour = GetComponentInParent<TrainBehaviour>();

            if (_trainBehaviour == null) { Debug.LogWarning($"TrainBehaviour not assigned or found. Please assign a TrainBehaviour", gameObject); }
        }

        protected virtual void GetWheelRadius()
        {
            if (_wheelRadii == null) { _wheelRadii = new List<float>(); }

            foreach (Transform wheel in _trainWheels)
            {
                Bounds wheelBounds = wheel.GetComponentInChildren<Renderer>().bounds;
                _wheelRadii.Add(wheelBounds.size.y);
            }
        }

        protected virtual void Update()
        {
            if (_trainBehaviour.EngineRunning == false) { return; }

            SpinWheels();
        }

        protected virtual void SpinWheels()
        {
            for (int i = 0; i < _wheelRadii.Count; i++)
            {
                float distanceTraveled = _trainBehaviour.CurrentSpeed * Time.deltaTime;
                float rotationInRadians = distanceTraveled / _wheelRadii[i];
                float rotationInDegrees = rotationInRadians * Mathf.Rad2Deg;

                _trainWheels[i].Rotate(rotationInDegrees, 0, 0);
            }
        }
    }
}
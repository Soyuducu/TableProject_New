// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.TrainController
{
    public class ExampleInputOld : MonoBehaviour
    {
        [SerializeField] protected TrainBehaviour _trainBehaviour;
        [SerializeField] protected KeyCode _throttleUp;
        [SerializeField] protected KeyCode _throttleDown;
        [SerializeField] protected KeyCode _stopTrain;

        public TrainBehaviour TrainBehaviour { get => _trainBehaviour; set => _trainBehaviour = value; }

        protected virtual void Awake() => GetRequiredComponents();

        protected virtual void GetRequiredComponents()
        {
            if (_trainBehaviour == null) { _trainBehaviour = GetComponent<TrainBehaviour>(); }
        }

        protected virtual void Update()
        {
            if (Input.GetKey(_throttleUp)) { _trainBehaviour.IncreaseThrottle(); }
            if (Input.GetKey(_throttleDown)) { _trainBehaviour.DecreaseThrottle(); }
            if (Input.GetKey(_stopTrain)) { _trainBehaviour.StopMoving(1.5f); }
        }
    }
}
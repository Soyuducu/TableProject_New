// Copyright (c) 2026 Enigma 23. All rights reserved.

using e23.Common;
using System.Linq;
using UnityEngine;

namespace e23.TrainController
{
    public class Booster : MonoBehaviour
    {
        [SerializeField] protected BoostSettings _boostSettings;

        protected int _ignoreCount = 0;
        protected TrainBehaviour _currentTrain;

        protected virtual void OnTriggerEnter(Collider collider)
        {
            var trainBehaviour = collider.gameObject.GetComponentInParent<TrainBehaviour>();
            
            if (trainBehaviour != null)
            {
                if (_currentTrain == trainBehaviour) { return; }

                _currentTrain = trainBehaviour;
                _ignoreCount = trainBehaviour.gameObject.GetComponentsInChildren<Collider>().Count();

                trainBehaviour.OneShotBoost(_boostSettings.BoostBy, _boostSettings.Duration, _boostSettings.TimeToBoost);
            }
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            _ignoreCount--;
            if (_ignoreCount <= 0) { _currentTrain = null; _ignoreCount = 0; }
        }
    }
}
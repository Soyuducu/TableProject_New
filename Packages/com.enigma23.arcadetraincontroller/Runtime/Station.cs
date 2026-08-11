// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections;
using UnityEngine;

namespace e23.TrainController
{
    public class Station : MonoBehaviour
    {
        [SerializeField] protected StationSettings _stationSettings;
        [Tooltip("Set this to true if a train starts off within the station collider.")]
        [SerializeField] protected bool _ignoreFirst = false;

        protected float _resumeSpeed = 0f;
        protected int _ignoreCount = 0;

        protected virtual void OnTriggerEnter(Collider other)
        {
            var trainBehaviour = other.gameObject.GetComponentInParent<TrainBehaviour>();
            if (trainBehaviour != null)
            {
                if (_ignoreCount > 0) { return; }
                
                if (typeof(MultiTrainBehaviour).IsAssignableFrom(trainBehaviour.GetType()) && _stationSettings.IgnoreAdditionalCarriages == true)
                {
                    MultiTrainBehaviour mtb = trainBehaviour as MultiTrainBehaviour;
                    _ignoreCount = mtb.CarriageCount;
                }

                if (_ignoreFirst) { return; }

                if (_stationSettings.UsePreviousSpeed) { _resumeSpeed = trainBehaviour.CurrentSpeed; }
                else { _resumeSpeed = _stationSettings.ResumeSpeed; }

                if (_stationSettings.InstantStop) 
                { 
                    trainBehaviour.StopInstantly();
                    StartCoroutine(OpenDoorsAfterWait(0, trainBehaviour));
                }
                else 
                { 
                    trainBehaviour.StopMoving(_stationSettings.TimeToStop);
                    StartCoroutine(OpenDoorsAfterWait(_stationSettings.TimeToStop + 0.5f, trainBehaviour));
                }

                if (_stationSettings.WaitDuration <= 0f) { return; }
                float resumeTime = _stationSettings.TimeToStop + _stationSettings.WaitDuration;
                StartCoroutine(ResumeAfterWait(resumeTime, trainBehaviour));
            }
        }

        protected virtual IEnumerator OpenDoorsAfterWait(float waitDuration, TrainBehaviour trainBehaviour)
        {
            yield return new WaitForSeconds(waitDuration);

            trainBehaviour.OpenDoors();
        }

        protected virtual IEnumerator ResumeAfterWait(float waitDuration, TrainBehaviour trainBehaviour)
        {
            yield return new WaitForSeconds(waitDuration);

            trainBehaviour.CloseDoors();

            if (_stationSettings.ReverseTrainDirection) { trainBehaviour.ReverseDirection(); }
            else { trainBehaviour.StartMoving(_resumeSpeed); }
        }

        protected virtual void OnTriggerExit(Collider other) 
        { 
            _ignoreFirst = false;

            if (_ignoreCount > 0) { _ignoreCount--; }
        }
    }
}
// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Linq;
using UnityEngine;

namespace e23.TrainController
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class AutoSwitch : MonoBehaviour
    {
        [Tooltip("Ignore the first train to collide.")]
        [SerializeField] protected bool _ignoreFirst = false;
        [Tooltip("Set this to false if you want the switch to trigger with carriages as well as the train.")]
        [SerializeField] protected bool _ignoreCarriages = true;
        [Tooltip("Set this to true if you want the train to immediately change to a new path.")]
        [SerializeField] protected bool _instantPathChange = false;
        [Tooltip("When a train is moving in reverse, the previous index will be used. Set this to true to force next index.")]
        [SerializeField] protected bool _alwaysUseNextIndex = false;

        protected int _ignoreCount = 0;
        protected TrainBehaviour _currentTrain;

        public bool IgnoreFirst { get => _ignoreFirst; set => _ignoreFirst = value; }
        public bool IgnoreCarriages { get => _ignoreCarriages; set => _ignoreCarriages = value; }
        public bool InstantPathChange { get => _instantPathChange; set => _instantPathChange = value; }
        public bool AlwaysUseNextIndex { get => _alwaysUseNextIndex; set => _alwaysUseNextIndex = value; }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (_ignoreFirst == true) { return; }
            if (_ignoreCarriages == true && _ignoreCount > 0) { return; }

            var trainBehaviour = other.gameObject.GetComponentInParent<TrainBehaviour>();

            if (trainBehaviour != null)
            {
                if (_ignoreCarriages == true && _currentTrain == trainBehaviour) { return; }

                _currentTrain = trainBehaviour;
                _ignoreCount = trainBehaviour.gameObject.GetComponentsInChildren<Collider>().Count() - 1;

                trainBehaviour.TrackPathManager.SetInstantPathChange(_instantPathChange);
                trainBehaviour.ResetPathChangeRangeStatus();

                if (trainBehaviour.IsMovingForward == true || _alwaysUseNextIndex == true) { trainBehaviour.TrackPathManager.NextIndex(); }
                else { trainBehaviour.TrackPathManager.PreviousIndex(); }
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            _ignoreFirst = false;

            if (_ignoreCarriages == false || _ignoreCount <= 0)
            {
                _ignoreCount = 0;
                _currentTrain = null;
                return;
            }

            _ignoreCount--;
        }
    }
}
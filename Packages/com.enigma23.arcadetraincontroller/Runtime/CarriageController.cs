// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace e23.TrainController
{
    [RequireComponent(typeof(TrainBehaviour))]
    public class CarriageController : MonoBehaviour
    {
        [Tooltip("If this field is left empty, GetComponentInParent is ran to find a TrainBehaviour.")]
        [SerializeField] protected TrainBehaviour _trainBehaviour;
        [SerializeField] protected List<Carriage> _carriages;

        [ContextMenu("Find Carriages")]
        protected virtual void FindCarriages() => _carriages = new List<Carriage>(GetComponentsInChildren<Carriage>());

        protected virtual void Awake() => GetRequiredComponents();
        protected virtual void OnEnable() => RegisterActions(true);
        protected virtual void OnDisable() => RegisterActions(false);

        protected virtual void GetRequiredComponents()
        {
            if (_trainBehaviour == null) { _trainBehaviour = GetComponentInParent<TrainBehaviour>(); }
        }

        protected virtual void RegisterActions(bool register)
        {
            _trainBehaviour.OnDoorsOpen -= OpenDoors;

            if (register == false) { return; }

            _trainBehaviour.OnDoorsOpen += OpenDoors;
        }

        protected virtual void OpenDoors(bool open)
        {
            foreach (Carriage carraige in _carriages)
            {
                carraige.OpenDoors(open);
            }
        }
    }
}
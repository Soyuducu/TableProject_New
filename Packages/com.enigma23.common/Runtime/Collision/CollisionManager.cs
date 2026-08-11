// Copyright (c) 2026 Enigma 23. All rights reserved.

using System;
using UnityEngine;

namespace e23.Common.Physics
{
    public class CollisionManager : MonoBehaviour
    {
        public Action<Collision> OnVehicleCollisionEnter;
        public Action<Collision> OnVehicleCollisionExit;
        public Action<Collision> OnVehicleCollisionStay;
        
        private void OnCollisionEnter(Collision collision) => OnVehicleCollisionEnter?.Invoke(collision);
        private void OnCollisionStay(Collision collision) => OnVehicleCollisionStay?.Invoke(collision);
        private void OnCollisionExit(Collision collision) => OnVehicleCollisionExit?.Invoke(collision);

        public Action<Collider> OnVehicleTriggerEnter;
        public Action<Collider> OnVehicleTriggerExit;
        public Action<Collider> OnVehicleTriggerStay;

        private void OnTriggerEnter(Collider collider) => OnVehicleTriggerEnter?.Invoke(collider);
        private void OnTriggerExit(Collider collider) => OnVehicleTriggerExit?.Invoke(collider);
        private void OnTriggerStay(Collider collider) => OnVehicleTriggerStay?.Invoke(collider);
    }
}
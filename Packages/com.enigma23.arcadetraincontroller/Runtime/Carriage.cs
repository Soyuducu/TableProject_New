// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace e23.TrainController
{
    public class Carriage : MonoBehaviour
    {
        [SerializeField] protected float _duration;
        [Tooltip("Each doors default position is cached in OnEnable, any value added here will be overwritten at runtime.")]
        [SerializeField] protected List<Door> _doors;

        public virtual void CreateDoor(Transform door, Vector3 closedPos, Vector3 openPos)
        {
            Door newDoor = new Door();
            newDoor.DoorTransform = door;
            newDoor.ClosedPosition = closedPos;
            newDoor.OpenPosition = openPos;

            if (_doors == null) { _doors = new List<Door>(); }
            _doors.Add(newDoor);
        }

        protected virtual void Awake() => GetRequiredComponents();

        protected virtual void GetRequiredComponents()
        {
            foreach (Door door in _doors) { door.ClosedPosition = door.DoorTransform.localPosition; }
        }

        public virtual void OpenDoors(bool open)
        {
            foreach (Door door in _doors)
            {
                Vector3 moveTo = open == true ? door.OpenPosition : door.ClosedPosition;
                StartCoroutine(OpenDoor(door, moveTo));
            }
        }

        protected virtual IEnumerator OpenDoor(Door door, Vector3 moveTo)
        {
            float time = 0;

            while (time < _duration)
            {
                door.DoorTransform.localPosition = Vector3.Lerp(door.DoorTransform.localPosition, moveTo, time / _duration);
                time += Time.deltaTime;

                yield return null;
            }

            door.DoorTransform.localPosition = moveTo;
        }
    }

    [System.Serializable]
    public class Door
    {
        public Transform DoorTransform;
        public Vector3 ClosedPosition = Vector3.zero;
        public Vector3 OpenPosition = Vector3.zero;
    }
}
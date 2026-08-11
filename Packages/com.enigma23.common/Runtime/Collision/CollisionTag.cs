// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.Common.Physics
{
    public class CollisionTag : MonoBehaviour
    {
        [SerializeField] private string collisionID = "";

        public string ID => collisionID;
    }
}
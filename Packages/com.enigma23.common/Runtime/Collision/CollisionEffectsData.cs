// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.Common.Physics
{
    [CreateAssetMenu(fileName = nameof(CollisionEffectsData), menuName = "e23/Common/Collision Effects Data", order = 5)]
    public class CollisionEffectsData : ScriptableObject
    {
        [SerializeField] private string effectsID = "";
        [SerializeField] private CollisionType collisionType;
        [SerializeField] private GameObject effectPrefab = null;
        [SerializeField] private float requiredSpeed;
        public string ID => effectsID;
        public CollisionType CollisionType => collisionType;
        public GameObject Prefab => effectPrefab;
        public float RequiredSpeed => requiredSpeed;
    }
}
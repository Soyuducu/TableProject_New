using UnityEngine;
using System.Collections;
using JetBrains.Annotations;
using Unity.VisualScripting;
using System.Linq;

public class DistanceCulling : MonoBehaviour
{
    void Update()
    {
        Camera camera = GetComponent<Camera>();
        float[] distances = camera.layerCullDistances;
        camera.layerCullSpherical = true;
        distances[LayerMask.NameToLayer("Trees")] = 1;
        distances[LayerMask.NameToLayer("Trolls")] = 0.5f;
        camera.layerCullDistances = distances;
    }
}

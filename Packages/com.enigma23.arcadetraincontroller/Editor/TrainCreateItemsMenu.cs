// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace e23.TrainController.Editor
{
    public class TrainCreateItemsMenu
    {
        [MenuItem("GameObject/e23/ATC/Spline", false, 0)]
        public static void CreateSplineNoVisual() => CreateSpline(false);
        [MenuItem("GameObject/e23/ATC/Spline and Track Visual", false, 1)]
        public static void CreateSplineWithVisual() => CreateSpline(true);

        private static void CreateSpline(bool withVisual, GameObject parentObject = null)
        {
            GameObject parent = Selection.activeGameObject;
            GameObject splineContainer = new GameObject("TrackSpline", typeof(SplineContainer));
            if (parentObject != null) 
            { 
                splineContainer.transform.SetParent(parentObject.transform);
                if (parent != null) { parentObject.transform.SetParent(parent.transform); }
            }
            else if (parent != null) { splineContainer.transform.SetParent(parent.transform); }

            if (withVisual == true)
            {
                parent = splineContainer;
                GameObject trackVisual = new GameObject("TrackVisual", typeof(SplineMeshRepeater));
                trackVisual.transform.SetParent(parent.transform);

                SplineContainer container = splineContainer.GetComponent<SplineContainer>();
                SplineMeshRepeater repeater = trackVisual.GetComponent<SplineMeshRepeater>();
                repeater.AssignSplineContainer(container);
            }

            Undo.RegisterCreatedObjectUndo(splineContainer, "Create Track Spline");
            if (parentObject != null) { Undo.RegisterCreatedObjectUndo(parentObject, "Create Track Spline"); }
            Selection.activeGameObject = splineContainer;
        }

        [MenuItem("GameObject/e23/ATC/Spline, Track Visual and Track Path Manager", false, 2)]
        public static void CreateSplineVisualPathManager()
        {
            GameObject newTrackSetup = new GameObject("Track");
            CreateSpline(true, newTrackSetup);
            CreatePathManager(newTrackSetup);
        }

        [MenuItem("GameObject/e23/ATC/Spline and Track Path Manager", false, 3)]
        public static void CreateSplinePathManager()
        {
            GameObject newTrackSetup = new GameObject("Track");
            CreateSpline(false, newTrackSetup);
            CreatePathManager(newTrackSetup);
        }

        [MenuItem("GameObject/e23/ATC/Track Visual", false, 4)]
        public static void CreateTrackVisual()
        {
            GameObject parent = Selection.activeGameObject;
            GameObject trackVisual = new GameObject("TrackVisual", typeof(SplineMeshRepeater));
            if (parent != null)
            {
                trackVisual.transform.SetParent(parent.transform);

                if (parent.TryGetComponent(out SplineContainer splineContainer))
                {
                    SplineMeshRepeater splineMeshRepeater = trackVisual.GetComponent<SplineMeshRepeater>();
                    splineMeshRepeater.AssignSplineContainer(splineContainer);
                }
            }

            Undo.RegisterCreatedObjectUndo(trackVisual, "Create Track Visual");
            Selection.activeGameObject = trackVisual;
        }

        [MenuItem("GameObject/e23/ATC/Track Path Manager", false, 5)]
        public static void CreatePathManager() => CreatePathManager(null);

        public static void CreatePathManager(GameObject parentObject = null)
        {
            GameObject parent = Selection.activeGameObject;
            GameObject pathManager = new GameObject("PathManager", typeof(TrackPathManager));

            if (parentObject != null) 
            { 
                pathManager.transform.SetParent(parentObject.transform);
                if (parent != null) { parentObject.transform.SetParent(parent.transform); }
            }
            else if (parent != null) { pathManager.transform.SetParent(parent.transform); }
                        
            if (pathManager.transform.parent != null)
            {
                SplineContainer container = pathManager.transform.parent.GetComponentInChildren<SplineContainer>();

                if (container != null)
                {
                    TrackPathManager trackPathManager = pathManager.GetComponent<TrackPathManager>();
                    trackPathManager.AssignSplineContainer(container);
                }
            }

            Undo.RegisterCreatedObjectUndo(pathManager, "Create Path Manager");

            Selection.activeGameObject = pathManager;
        }

        [MenuItem("GameObject/e23/ATC/Station", false, 6)]
        public static void CreateStation()
        {
            GameObject parent = Selection.activeGameObject;
            GameObject station = new GameObject("Station", typeof(Station));
            Rigidbody rigidbody = station.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            BoxCollider boxCollider = station.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            if (parent != null) { station.transform.SetParent(parent.transform); }

            Undo.RegisterCreatedObjectUndo(station, "Create Station");

            Selection.activeObject = station;
        }

        [MenuItem("GameObject/e23/ATC/Track Switch", false, 7)]
        public static void CreateTrackSwitch()
        {
            GameObject parent = Selection.activeGameObject;
            GameObject trackSwitch = new GameObject("TrackSwitch", typeof(TrackSwitch));
            
            if (parent != null) { trackSwitch.transform.SetParent(parent.transform); }

            Undo.RegisterCreatedObjectUndo(trackSwitch, "Create Track Switch");

            Selection.activeGameObject = trackSwitch;
        }

        [MenuItem("GameObject/e23/ATC/Auto Switch", false, 8)]
        public static void CreateAutoSwitch()
        {
            GameObject parent = Selection.activeGameObject;
            GameObject autoSwitch = new GameObject("AutoSwitch", typeof(Rigidbody));
            Rigidbody rigidbody = autoSwitch.GetComponent<Rigidbody>();
            rigidbody.useGravity = false;
            BoxCollider boxCollider = autoSwitch.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            autoSwitch.AddComponent<AutoSwitch>();

            if (parent != null) { autoSwitch.transform.SetParent(parent.transform); }

            Undo.RegisterCreatedObjectUndo(autoSwitch, "Create Auto Switch");

            Selection.activeGameObject = autoSwitch;
        }
    }
}
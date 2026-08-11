// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace e23.TrainController.Editor
{
    [CustomEditor(typeof(TrackPathManager))]
    [CanEditMultipleObjects]
    public class TrackPathManagerEditor : UnityEditor.Editor
    {
        private const int SAMPLES_PER_SEGMENT = 20;
        private const float LINE_WIDTH = 6f;
        private static readonly Color PATH_COLOUR = new Color(0.2f, 0.8f, 0.2f, 1f);
        private static readonly Color KNOT_COLOUR = new Color(1f, 0.6f, 0f, 1f);
        private const float KNOT_SIZE = 1.15f;
        private static NativeSpline _tempSpline;
        private static bool _tempCreated = false;
        
        private bool _containerWarning = false;

        private void OnDisable()
        {
            if (_tempCreated ==  false) { return; }

            _tempSpline.Dispose();
            _tempCreated = false;
        }

        private void OnSceneGUI()
        {
            var manager = (TrackPathManager) target;

            if (_tempCreated == false) { CreateTempSpline(manager); }            
            DrawPath(manager);
        }

        private void CreateTempSpline(TrackPathManager trackPathManager)
        {
            if (trackPathManager.SplineContainer == null)
            {
                if (_containerWarning == true) { return; }

                _containerWarning = true;
                Debug.LogWarning($"To visualise the active path, assign a SplineContainer to {trackPathManager.gameObject.name}", trackPathManager.gameObject);
                return; 
            }

            _containerWarning = false;

            if (trackPathManager.SplineContainer.Splines.First().Knots.Count() == 0) { return; }

            if ((trackPathManager.Switches != null && trackPathManager.CurrentPathIndex > trackPathManager.Switches.Count) || trackPathManager.CurrentPathIndex < 0)
            {
                Debug.LogWarning($"Track Path Manager does not have a path at index: {trackPathManager.CurrentPathIndex}. There are only {trackPathManager.Switches.Count + 1} paths (0 to {trackPathManager.Switches.Count})");
                return;
            }

            SplinePath splinePath;

            if (trackPathManager.Switches == null || trackPathManager.Switches.Count == 0)
            {
                var slices = new List<SplineSlice<Spline>>();
                if (trackPathManager.Closed == false)
                {
                    slices.Add(new SplineSlice<Spline>
                    (
                        trackPathManager.SplineContainer.Splines.First(),
                        new SplineRange(0, trackPathManager.SplineContainer.Splines[0].Count)
                    ));
                }
                else
                {
                    slices.Add(new SplineSlice<Spline>
                    (
                        trackPathManager.SplineContainer.Splines.First(),
                        new SplineRange(0, trackPathManager.SplineContainer.Splines[0].Knots.Count() + 1)
                    ));
                }

                splinePath = new SplinePath(slices);
            }
            else
            {
                var slices = new List<SplineSlice<Spline>>();

                for (int i = 0; i < trackPathManager.Switches.Count; i++)
                {
                    var splineRangeData = trackPathManager.Switches[i].SplineRangeData(trackPathManager.CurrentPathIndex);
                    if (splineRangeData.Ignore) { continue; }

                    Spline spline = trackPathManager.SplineContainer.Splines[splineRangeData.Spline];

                    var slice = new SplineSlice<Spline>
                    (
                        spline,
                        new SplineRange(splineRangeData.StartKnot, splineRangeData.knotCount)
                    );

                    slices.Add(slice);
                }

                splinePath = new SplinePath(slices);
            }

            _tempSpline = new NativeSpline(splinePath, trackPathManager.SplineContainer.transform.localToWorldMatrix, Allocator.TempJob);
            _tempCreated = true;
        }

        private void DrawPath(TrackPathManager trackPathManager)
        {
            if (_tempCreated == false) { return; }

            int curveCount = _tempSpline.Count - (trackPathManager.Closed ? 0 : 1);

            if (curveCount <= 0) { return; }

            Handles.color = PATH_COLOUR;

            // Sample points along the spline and draw lines between them
            Vector3 previousPoint = Vector3.zero;
            int totalSamples = curveCount * SAMPLES_PER_SEGMENT;

            for (int i = 0; i <= totalSamples; i++)
            {
                float t = (float)i / totalSamples;

                SplineUtility.Evaluate
                (
                    _tempSpline, 
                    t,
                    out float3 position,
                    out float3 tangent,
                    out float3 up
                );

                Vector3 currentPoint = position;

                if (i > 0) { Handles.DrawAAPolyLine(LINE_WIDTH, previousPoint, currentPoint); }

                previousPoint = currentPoint;
            }

            Handles.color = KNOT_COLOUR;
            for (int i = 0; i < _tempSpline.Count; i++)
            {
                Vector3 knotPos = _tempSpline[i].Position;
                Handles.SphereHandleCap(0, knotPos, Quaternion.identity, KNOT_SIZE, EventType.Repaint);
            }

            _tempSpline.Dispose();
            _tempCreated = false;
        }
    }
}
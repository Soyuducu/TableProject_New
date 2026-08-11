// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace e23.TrainController.Editor
{
    public class TrainBuilderSettings : ScriptableObject
    {
        [HideInInspector][SerializeField] private string _trainName;
        [HideInInspector][SerializeField] private GameObject _trainModel;
        [HideInInspector][SerializeField] private string _bodyName;
        [HideInInspector][SerializeField] private TrainBehaviourSettings _trainBehaviourSettings;
        [HideInInspector][SerializeField] private List<string> _wheelNames;
        [HideInInspector][SerializeField] private bool _addCarriages;
        [HideInInspector][SerializeField] private List<GameObject> _carriages;
        [HideInInspector][SerializeField] private List<string> _carriageWheelNames;
        [HideInInspector][SerializeField] private bool _addColliders;
        [HideInInspector][SerializeField] private bool _addExampleInput;
        [HideInInspector][SerializeField] private bool _addNewInput;

        public string TrainName
        { get => _trainName; set => _trainName = value; }

        public GameObject TrainModel
        { get => _trainModel; set => _trainModel = value; }

        public string BodyName
        { get => _bodyName; set => _bodyName = value; }

        public TrainBehaviourSettings TrainBehaviourSettings
        { get => _trainBehaviourSettings; set => _trainBehaviourSettings = value; }

        public List<string> WheelNames
        { get => _wheelNames; set => _wheelNames = value; }

        public bool AddCarriages
        { get => _addCarriages; set => _addCarriages = value; }

        public List<GameObject> Carriages
        { get => _carriages; set => _carriages = value; }

        public List<string> CarriageWheelNames
        { get => _carriageWheelNames; set => _carriageWheelNames = value; }

        public bool AddColliders
        { get => _addColliders; set => _addColliders = value; }

        public bool AddExampleInput
        { get => _addExampleInput; set => _addExampleInput = value; }

        public bool AddNewInput
        { get => _addNewInput; set => _addNewInput = value; }
    }
}
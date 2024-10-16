﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System.Linq;
using JayoMidiPlugin.VNyanPluginHelper;

namespace JayoMidiPlugin
{

    public class MidiManager : MonoBehaviour
    {

        private string midiInput;
        private InputDevice midiInputDevice;
        private JayoMidiPlugin plugin;
        private VNyanHelper _VNyanHelper;

        public void Awake()
        {
            Debug.Log("Midi Manager Awake!");
            _VNyanHelper = new VNyanHelper();
            plugin = GetComponent<JayoMidiPlugin>();
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        private void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("Exiting Play Mode");
                deInitMidi();
            }
        }

        private void OnDestroy()
        {
            deInitMidi();
        }

        private void OnApplicationQuit()
        {
            deInitMidi();
        }

        public List<InputDevice> listInputDevices()
        {
            return InputDevice.GetAll().ToList();
        }

        public List<OutputDevice> listOutputDevices()
        {
            return OutputDevice.GetAll().ToList();
        }

        public bool initMidi(string midiInputName)
        {

            try
            {
                midiInput = midiInputName;
                midiInputDevice = InputDevice.GetByName(midiInput);
                Debug.Log($"Loaded Input Device: {midiInputDevice.Name}");
                midiInputDevice.EventReceived += OnEventReceived;
                midiInputDevice.StartEventsListening();
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Couldn't initialize MIDI Controller '{midiInput}': {e.Message}");
                return false;
            }
        }

        public bool deInitMidi()
        {
            try
            {
                midiInputDevice.StopEventsListening();
                midiInputDevice.EventReceived -= OnEventReceived;
                midiInputDevice.Dispose();
                Debug.Log($"Disposed of MIDI Controller");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Couldn't destroy MIDI Controller '{this.midiInput}': {e.Message}");
                return false;
            }
        }

        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            var Event = e.Event;
            if (Event is NoteOnEvent noteOnEvent)
            {
                try
                {
                    Debug.Log($"A Note was pressed: {noteOnEvent.NoteNumber} , {noteOnEvent.Velocity}");
                    plugin.setLastNoteOnTrigger($"_xjm_n1_{noteOnEvent.NoteNumber}", noteOnEvent.Velocity);
                    _VNyanHelper.setVNyanParameterFloat($"_xjm_note_{noteOnEvent.NoteNumber}", noteOnEvent.Velocity);
                    _VNyanHelper.callTrigger($"_xjm_n1_{noteOnEvent.NoteNumber}", 0, 0, 0, "", "", "");
                }
                catch (Exception ex)
                {
                    Debug.Log($"Couldn't handle note press: {ex.Message}");
                }
                
            }
            if (Event is NoteOffEvent noteOffEvent)
            {
                Debug.Log($"A Note was released: {noteOffEvent.NoteNumber} , {noteOffEvent.Velocity}");
                plugin.setLastNoteOffTrigger($"_xjm_n0_{noteOffEvent.NoteNumber}", noteOffEvent.Velocity);
                _VNyanHelper.setVNyanParameterFloat($"_xjm_note_{noteOffEvent.NoteNumber}", noteOffEvent.Velocity);
                _VNyanHelper.callTrigger($"_xjm_n0_{noteOffEvent.NoteNumber}", 0, 0, 0, "", "", "");
            }
            if (Event is ControlChangeEvent controlChangeEvent)
            {
                Debug.Log($"A Control was changed: {controlChangeEvent.ControlNumber} , {controlChangeEvent.ControlValue}");
                plugin.setLastControlChangeTrigger($"_xjm_ct_{controlChangeEvent.ControlNumber}", controlChangeEvent.ControlValue);
                _VNyanHelper.setVNyanParameterFloat($"_xjm_control_{controlChangeEvent.ControlNumber}", controlChangeEvent.ControlValue);
                _VNyanHelper.callTrigger($"_xjm_ct_{controlChangeEvent.ControlNumber}", 0, 0, 0, "", "", "");
            }
        }



    }
}



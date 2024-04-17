using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using VNyanInterface;
using System.Linq;

namespace JayoMidiPlugin
{

    public class MidiManager : MonoBehaviour
    {

        private void callVNyanTrigger(string triggerName)
        {
            shouldSendTrigger = triggerName;
        }

        private void Start()
        {

            cvt = triggerSender.GetComponent<CallVNyanTrigger>();
            plugin = GetComponent<JayoMidiPlugin>();

            VNyanHelper.setVNyanParameterString("MidiMessage", "Midi Manager Loaded!");
            Debug.Log("Midi Manager Loaded!");
            
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        private void Update()
        {
            if (shouldSendTrigger != "")
            {
                cvt.TriggerName = shouldSendTrigger;
                triggerSender.SetActive(true);
                triggerSender.SetActive(false);
                shouldSendTrigger = "";
            }
        }

        private void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("Exiting Play Mode");
                deInitMidi();
            }
        }

        private void OnEnable()
        {
            Debug.Log("Midi Manager: Enable Called");
            VNyanHelper.setVNyanParameterString("MidiMessage", "Enable Called!!");
        }

        private void OnDisable()
        {
            deInitMidi();
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
                Debug.Log("Flag A");
                midiInput = midiInputName;
                midiInputDevice = InputDevice.GetByName(midiInput);
                Debug.Log($"Loaded Input Device: {midiInputDevice.Name}");
                midiInputDevice.EventReceived += OnEventReceived;
                midiInputDevice.StartEventsListening();

                VNyanHelper.setVNyanParameterString("MidiMessage", "MIDI Device ready!");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Couldn't initialize MIDI Controller '{midiInput}': {e.Message}");
                VNyanHelper.setVNyanParameterString("MidiError", e.ToString());
                return false;
            }
        }

        public bool deInitMidi()
        {
            try
            {
                Debug.Log("Flag 2A");
                Debug.Log($"Loaded Input Device: {midiInputDevice.Name}");
                Debug.Log("Flag 2B");
                midiInputDevice.StopEventsListening();
                Debug.Log("Flag 2C");
                midiInputDevice.EventReceived -= OnEventReceived;
                Debug.Log("Flag 2D");
                midiInputDevice.Dispose();
                Debug.Log("Flag 2E");
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
            Debug.Log("Flag 3X");
            if (Event is NoteOnEvent noteOnEvent)
            {
                try
                {
                    Debug.Log($"A Note was pressed: {noteOnEvent.NoteNumber} , {noteOnEvent.Velocity}");
                    Debug.Log("Flag 3X2");
                    plugin.setLastNoteOnTrigger($"_xjm_n1_{noteOnEvent.NoteNumber}", noteOnEvent.Velocity);
                    Debug.Log("Flag 3Y");
                    VNyanHelper.setVNyanParameterFloat($"_xjm_note_{noteOnEvent.NoteNumber}", noteOnEvent.Velocity);
                    Debug.Log("Flag 3Z");
                    callVNyanTrigger($"_xjm_n1_{noteOnEvent.NoteNumber}");
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
                VNyanHelper.setVNyanParameterFloat($"_xjm_note_{noteOffEvent.NoteNumber}", noteOffEvent.Velocity);
                callVNyanTrigger($"_xjm_n0_{noteOffEvent.NoteNumber}");
            }
            if (Event is ControlChangeEvent controlChangeEvent)
            {
                Debug.Log($"A Control was changed: {controlChangeEvent.ControlNumber} , {controlChangeEvent.ControlValue}");
                plugin.setLastControlChangeTrigger($"_xjm_ct_{controlChangeEvent.ControlNumber}", controlChangeEvent.ControlValue);
                VNyanHelper.setVNyanParameterFloat($"_xjm_control_{controlChangeEvent.ControlNumber}", controlChangeEvent.ControlValue);
                callVNyanTrigger($"_xjm_ct_{controlChangeEvent.ControlNumber}");
            }
        }

        public GameObject triggerSender;

        private CallVNyanTrigger cvt;
        [SerializeField]
        private string midiInput { get; set; }
        private string shouldSendTrigger { get; set; }
        private InputDevice midiInputDevice { get; set; }
        private JayoMidiPlugin plugin { get; set; }

    }
}



using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System.Linq;

namespace JayoMidiPlugin
{

    public class MidiManager : MonoBehaviour
    {

        private string midiInput;
        private InputDevice midiInputDevice;
        private JayoMidiPlugin plugin;

        public void Awake()
        {
            Logger.LogInfo("Midi Manager Awake!");
            plugin = GetComponent<JayoMidiPlugin>();
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
                Logger.LogInfo($"Loaded Input Device: {midiInputDevice.Name}");
                midiInputDevice.EventReceived += OnEventReceived;
                midiInputDevice.StartEventsListening();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError($"Couldn't initialize MIDI Controller '{midiInput}': {e.Message}");
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
                Logger.LogInfo($"Disposed of MIDI Controller");
                return true;
            }
            catch (Exception e)
            {
                Logger.LogInfo($"Couldn't destroy MIDI Controller '{this.midiInput}': {e.Message}");
                return false;
            }
        }

        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            var Event = e.Event;
            if (Event is NoteOnEvent noteOnEvent)
            {
                Logger.LogInfo($"A Note was pressed: {noteOnEvent.NoteNumber} , {noteOnEvent.Velocity}");
                plugin.setLastNoteOnTrigger($"_xjm_n1_{noteOnEvent.NoteNumber}", noteOnEvent.Velocity);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat($"_xjm_note_{noteOnEvent.NoteNumber}", noteOnEvent.Velocity);
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger($"_xjm_n1_{noteOnEvent.NoteNumber}", 0, 0, 0, "", "", "");

            }
            if (Event is NoteOffEvent noteOffEvent)
            {
                Logger.LogInfo($"A Note was released: {noteOffEvent.NoteNumber} , {noteOffEvent.Velocity}");
                plugin.setLastNoteOffTrigger($"_xjm_n0_{noteOffEvent.NoteNumber}", noteOffEvent.Velocity);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat($"_xjm_note_{noteOffEvent.NoteNumber}", noteOffEvent.Velocity);
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger($"_xjm_n0_{noteOffEvent.NoteNumber}", 0, 0, 0, "", "", "");
            }
            if (Event is ControlChangeEvent controlChangeEvent)
            {
                Logger.LogInfo($"A Control was changed: {controlChangeEvent.ControlNumber} , {controlChangeEvent.ControlValue}");
                plugin.setLastControlChangeTrigger($"_xjm_ct_{controlChangeEvent.ControlNumber}", controlChangeEvent.ControlValue);
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat($"_xjm_control_{controlChangeEvent.ControlNumber}", controlChangeEvent.ControlValue);
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger($"_xjm_ct_{controlChangeEvent.ControlNumber}", 0, 0, 0, "", "", "");
            }
        }
    }
}



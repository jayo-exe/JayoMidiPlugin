using JetBrains.Annotations;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VNyanInterface;

namespace JayoMidiPlugin
{
    public class JayoMidiPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
        public GameObject windowPrefab;

        private GameObject window;
        private MainThreadDispatcher mainThread;
        private VNyanHelper _VNyanHelper;
        private VNyanTriggerDispatcher triggerDispatcher;
        private MidiManager midiManager;
        private List<string> inputDeviceNames;
        private List<string> outputDeviceNames;

        // Settings
        [SerializeField]
        private string midiInputDevice;

        [SerializeField]
        private string midiOutputDevice;

        private string lastNoteOnTrigger;
        private string lastNoteOffTrigger;
        private string lastControlChangeTrigger;
        private SevenBitNumber lastNoteOnValue;
        private SevenBitNumber lastNoteOffValue;
        private SevenBitNumber lastControlChangeValue;
        private bool shouldUpdateTriggers;
        private string shouldCopyToClipboard;

        private Text lastNoteOnText;
        private Text lastNoteOffText;
        private Text lastControlChangeText;
        private Text lastNoteOnValueText;
        private Text lastNoteOffValueText;
        private Text lastControlChangeValueText;

        public void Start()
        {
            shouldCopyToClipboard = "";
            lastNoteOnTrigger = "(none)";
            lastNoteOffTrigger = "(none)";
            lastControlChangeTrigger = "(none)";
            lastNoteOnValue = (SevenBitNumber)0;
            lastNoteOffValue = (SevenBitNumber)0;
            lastControlChangeValue = (SevenBitNumber)0;
            shouldUpdateTriggers = true;
        }

        public void Awake()
        {
            
            Debug.Log($"MIDI Plugin is Awake!");
            _VNyanHelper = new VNyanHelper();

            Debug.Log($"Loading Settings");
            // Load settings
            loadPluginSettings();

            Debug.Log($"Beginning Plugin Setup");
            
            mainThread = gameObject.AddComponent<MainThreadDispatcher>();
            triggerDispatcher = gameObject.AddComponent<VNyanTriggerDispatcher>();
            midiManager = gameObject.AddComponent<MidiManager>();

            try
            {
                window = _VNyanHelper.pluginSetup(this, "Jayo's MIDI Plugin", windowPrefab);
            } catch(Exception e)
            {
                Debug.Log(e.ToString());
            }
            
            // Hide the window by default
            if (window != null)
            {
                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                window.SetActive(false);

                lastNoteOnText = window.transform.Find("Panel/TriggerList/LastNoteOn").GetComponent<Text>();
                lastNoteOffText = window.transform.Find("Panel/TriggerList/LastNoteOff").GetComponent<Text>();
                lastControlChangeText = window.transform.Find("Panel/TriggerList/LastControlChange").GetComponent<Text>();
                lastNoteOnValueText = window.transform.Find("Panel/TriggerList/LastNoteOnValue").GetComponent<Text>();
                lastNoteOffValueText = window.transform.Find("Panel/TriggerList/LastNoteOffValue").GetComponent<Text>();
                lastControlChangeValueText = window.transform.Find("Panel/TriggerList/LastControlChangeValue").GetComponent<Text>();

                setStatusTitle("Initializing");
                setStatusMessage("");
                //load MIDI Devices list
                try
                {
                    Debug.Log($"Loading MIDI Input List");
                    setStatusTitle("Loading MIDI Input List");
                    List<Dropdown.OptionData> inputOptions = buildInputOptionsList();
                    Dropdown InputSelectDropdown = window.transform.Find("Panel/Midi Source/Source Dropdown").GetComponent<Dropdown>();

                    InputSelectDropdown?.ClearOptions();
                    InputSelectDropdown?.AddOptions(inputOptions);

                    InputSelectDropdown?.onValueChanged.AddListener((v) => { Debug.Log($"Value changed: {v}; {inputDeviceNames[v]}");  midiInputDevice = inputDeviceNames[v]; });
                    InputSelectDropdown?.SetValueWithoutNotify(inputDeviceNames.IndexOf(midiInputDevice));

                    window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(() => { closePluginWindow(); });
                    window.transform.Find("Panel/StatusControls/ConnectButton").GetComponent<Button>().onClick.AddListener(() => { connectToMidiDevice(); });
                    window.transform.Find("Panel/StatusControls/DisconnectButton").GetComponent<Button>().onClick.AddListener(() => { DisconnectFromMidiDevice(); });
                    window.transform.Find("Panel/TriggerList/CopyNoteOnBtn").GetComponent<Button>().onClick.AddListener(() => { copyLastNoteOnTrigger(); });
                    window.transform.Find("Panel/TriggerList/CopyNoteOffBtn").GetComponent<Button>().onClick.AddListener(() => { copyLastNoteOffTrigger(); });
                    window.transform.Find("Panel/TriggerList/CopyControlChangeBtn").GetComponent<Button>().onClick.AddListener(() => { copyLastControlChangeTrigger(); });
                }
                catch (Exception e)
                {
                    Debug.Log($"Couldn't initialize MIDI Input List: {e.Message}");
                    setStatusTitle("Couldn't initialize MIDI Input List");
                    setStatusMessage($"initError: {e.Message}\n{e.StackTrace}");
                    Debug.Log(e.ToString());
                }

                try
                {
                    connectToMidiDevice();
                }
                catch (Exception e)
                {
                    Debug.Log($"Couldn't initialize MIDI Input Connection: {e.Message}");
                    setStatusTitle("Couldn't initialize MIDI Input Connection");
                    setStatusMessage($"connError: {e.Message}\n{e.StackTrace}");
                    GUIUtility.systemCopyBuffer = e.StackTrace;
                }
            }
        }

        public void Update()
        {
            if(shouldUpdateTriggers)
            {
                lastNoteOnText.text = lastNoteOnTrigger;
                lastNoteOffText.text = lastNoteOffTrigger;
                lastControlChangeText.text = lastControlChangeTrigger;
                lastNoteOnValueText.text = $"({lastNoteOnValue})";
                lastNoteOffValueText.text = $"({lastNoteOffValue})";
                lastControlChangeValueText.text = $"({lastControlChangeValue})";
                shouldUpdateTriggers = false;
            }

            if(shouldCopyToClipboard != "")
            {
                GUIUtility.systemCopyBuffer = shouldCopyToClipboard;
                shouldCopyToClipboard = "";
            }

        }

        private List<Dropdown.OptionData> buildInputOptionsList()
        {
            Debug.Log($"Checking for Input Devices...");
            List<InputDevice> inputDevices = listInputDevices();
            List<Dropdown.OptionData> optionsList = new List<Dropdown.OptionData>();

            inputDeviceNames = new List<string>();
            
            inputDeviceNames.Add("<None>");
            optionsList.Add(new Dropdown.OptionData("<None>"));

            foreach (InputDevice device in inputDevices)
            {
                inputDeviceNames.Add(device.Name);
                optionsList.Add(new Dropdown.OptionData(device.Name));
            }

            return optionsList;
        }

        private List<Dropdown.OptionData> buildOutputOptionsList()
        {
            List<OutputDevice> outputDevices = listOutputDevices();
            List<Dropdown.OptionData> optionsList = new List<Dropdown.OptionData>();

            outputDeviceNames = new List<string>();
            
            outputDeviceNames.Add("<None>");
            optionsList.Add(new Dropdown.OptionData("<None>"));

            foreach (OutputDevice device in outputDevices)
            {
                outputDeviceNames.Add(device.Name);
                optionsList.Add(new Dropdown.OptionData(device.Name));
            }

            return optionsList;
        }


        private void loadPluginSettings()
        {
            // Get settings in dictionary
            Dictionary<string, string> settings = _VNyanHelper.loadPluginSettingsData("JayoMidiPlugin.cfg");
            if (settings != null)
            {
                // Read string value
                settings.TryGetValue("MidiInputDevice", out midiInputDevice);
                settings.TryGetValue("MidiOutputDevice", out midiOutputDevice);

            } else
            {
                midiInputDevice = "<None>";
                midiOutputDevice = "<None>";
            }
        }

        /// <summary>
        /// Called when VNyan is shutting down
        /// </summary>
        private void OnApplicationQuit()
        {
            // Save settings
            savePluginSettings();
        }

        private void savePluginSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["MidiInputDevice"] = midiInputDevice;
            settings["MidiOutputDevice"] = midiOutputDevice;

            _VNyanHelper.savePluginSettingsData("JayoMidiPlugin.cfg", settings);
        }

        public void pluginButtonClicked()
        {
            // Flip the visibility of the window when plugin window button is clicked
            Debug.Log("plugin button clicked");
            if (window != null)
            {
                window.SetActive(!window.activeSelf);
                if(window.activeSelf)
                    window.transform.SetAsLastSibling();
            }
                
        }

        private void setStatusTitle(string titleText)
        {
            Text StatusTitle = window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<Text>();
            StatusTitle.text = titleText;
        }

        private void setStatusMessage(string messageText)
        {
            Text StatusMessage = window.transform.Find("Panel/StatusControls/Status Error Text").GetComponent<Text>();
            StatusMessage.text = messageText;
        }

        public List<InputDevice> listInputDevices()
        {
            return InputDevice.GetAll().ToList();
        }

        public List<OutputDevice> listOutputDevices()
        {
            return OutputDevice.GetAll().ToList();
        }

        public void closePluginWindow()
        {
            window.SetActive(false);
        }

        public void connectToMidiDevice()
        {
            Debug.Log($"Initializing MIDI Connection");
            if(midiInputDevice == "<None>" || midiInputDevice == "" || midiInputDevice == null)
            {
                setStatusTitle("Select a MIDI Device");
                return;
            }

            setStatusTitle("Initializing MIDI Connection");
            if (GetComponent<MidiManager>().initMidi(midiInputDevice))
            {
                window.transform.Find("Panel/StatusControls/ConnectButton").gameObject.SetActive(false);
                window.transform.Find("Panel/StatusControls/DisconnectButton").gameObject.SetActive(true);
                window.transform.Find("Panel/Midi Source/Source Dropdown").GetComponent<Dropdown>().interactable = false;
                setStatusTitle("Midi Connected and Listening!");
                setStatusMessage("");
            } else
            {
                setStatusTitle("Unable to connect to MIDI Device");
            }
        }

        public void DisconnectFromMidiDevice()
        {
            setStatusTitle("Disconnecting MIDI Connection");
            if (GetComponent<MidiManager>().deInitMidi())
            {
                window.transform.Find("Panel/StatusControls/ConnectButton").gameObject.SetActive(true);
                window.transform.Find("Panel/StatusControls/DisconnectButton").gameObject.SetActive(false);
                window.transform.Find("Panel/Midi Source/Source Dropdown").GetComponent<Dropdown>().interactable = true;
                setStatusTitle("Midi Device Disconnected");
            }
            else
            {
                setStatusTitle("Unable to disconnect MIDI Device");
            }
        }

        public void setLastNoteOnTrigger(string triggerName, SevenBitNumber value)
        {
            lastNoteOnTrigger = triggerName;
            lastNoteOnValue = value;
            shouldUpdateTriggers = true;
        }

        public void copyLastNoteOnTrigger()
        {
            shouldCopyToClipboard = lastNoteOnTrigger;
        }

        public void setLastNoteOffTrigger(string triggerName, SevenBitNumber value)
        {
            lastNoteOffTrigger = triggerName;
            lastNoteOffValue = value;
            shouldUpdateTriggers = true;
        }

        public void copyLastNoteOffTrigger()
        {
            shouldCopyToClipboard = lastNoteOffTrigger;
        }

        public void setLastControlChangeTrigger(string triggerName, SevenBitNumber value)
        {
            lastControlChangeTrigger = triggerName;
            lastControlChangeValue = value;
            shouldUpdateTriggers = true;
        }

        public void copyLastControlChangeTrigger()
        {
            shouldCopyToClipboard = lastControlChangeTrigger;
        }

    }
}

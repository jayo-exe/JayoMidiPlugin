using JayoMidiPlugin.Util;
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
using TMPro;

namespace JayoMidiPlugin
{
    public class JayoMidiPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
        public GameObject windowPrefab;

        private GameObject window;
        private PluginUpdater updater;
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

        private TMP_Text statusText;
        private TMP_Text errorText;
        private TMP_Text lastNoteOnText;
        private TMP_Text lastNoteOffText;
        private TMP_Text lastControlChangeText;
        private TMP_Text lastNoteOnValueText;
        private TMP_Text lastNoteOffValueText;
        private TMP_Text lastControlChangeValueText;
        private TMP_Dropdown inputSelectDropdown;
        private Button connectButton;
        private Button disconnectButton;
        private Button copyNoteOnButton;
        private Button copyNoteOffButton;
        private Button copyControlChangeButton;

        private string currentVersion = "v0.3.0";
        private string repoName = "jayo-exe/JayoMidiPlugin";
        private string updateLink = "https://jayo-exe.itch.io/midi-plugin-for-vnyan";

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
            
            Logger.LogInfo($"MIDI Plugin is Awake!");

            updater = new PluginUpdater(repoName, currentVersion, updateLink);
            updater.OpenUrlRequested += (url) => MainThreadDispatcher.Enqueue(() => { Application.OpenURL(url); });

            Logger.LogInfo($"Loading Settings");
            // Load settings
            loadPluginSettings();
            updater.CheckForUpdates();

            Logger.LogInfo($"Beginning Plugin Setup");
            
            midiManager = gameObject.AddComponent<MidiManager>();

            try
            {
                VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("Jayo's MIDI Plugin", this);
                window = (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
            } catch(Exception e)
            {
                Logger.LogError(e.ToString());
            }
            
            // Hide the window by default
            if (window != null)
            {
                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                window.SetActive(false);

                statusText = window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<TMP_Text>();
                errorText = window.transform.Find("Panel/StatusControls/Status Error Text").GetComponent<TMP_Text>();
                lastNoteOnText = window.transform.Find("Panel/TriggerList/LastNoteOn").GetComponent<TMP_Text>();
                lastNoteOffText = window.transform.Find("Panel/TriggerList/LastNoteOff").GetComponent<TMP_Text>();
                lastControlChangeText = window.transform.Find("Panel/TriggerList/LastControlChange").GetComponent<TMP_Text>();
                lastNoteOnValueText = window.transform.Find("Panel/TriggerList/LastNoteOnValue").GetComponent<TMP_Text>();
                lastNoteOffValueText = window.transform.Find("Panel/TriggerList/LastNoteOffValue").GetComponent<TMP_Text>();
                lastControlChangeValueText = window.transform.Find("Panel/TriggerList/LastControlChangeValue").GetComponent<TMP_Text>();
                
                inputSelectDropdown = window.transform.Find("Panel/Midi Source/Source/Source Dropdown").GetComponent<TMP_Dropdown>();
                connectButton = window.transform.Find("Panel/Midi Source/ConnectButton").GetComponent<Button>();
                disconnectButton = window.transform.Find("Panel/Midi Source/DisconnectButton").GetComponent<Button>();

                copyNoteOnButton = window.transform.Find("Panel/TriggerList/CopyNoteOnBtn").GetComponent<Button>();
                copyNoteOffButton = window.transform.Find("Panel/TriggerList/CopyNoteOffBtn").GetComponent<Button>();
                copyControlChangeButton = window.transform.Find("Panel/TriggerList/CopyControlChangeBtn").GetComponent<Button>();


                setStatusTitle("Initializing");
                setStatusMessage("");
                //load MIDI Devices list
                try
                {
                    updater.PrepareUpdateUI(
                        window.transform.Find("Panel/UpdateRow/VersionText").gameObject,
                        window.transform.Find("Panel/UpdateRow/UpdateText").gameObject,
                        window.transform.Find("Panel/UpdateRow/UpdateButton").gameObject
                    );

                    Debug.Log($"Loading MIDI Input List");
                    setStatusTitle("Loading MIDI Input List");
                    Logger.LogInfo("A");
                    inputSelectDropdown.ClearOptions();
                    inputSelectDropdown.AddOptions(buildInputOptionsList());
                    Logger.LogInfo("B");
                    inputSelectDropdown.onValueChanged.AddListener((v) => { Debug.Log($"Value changed: {v}; {inputDeviceNames[v]}");  midiInputDevice = inputDeviceNames[v]; });
                    inputSelectDropdown.SetValueWithoutNotify(inputDeviceNames.IndexOf(midiInputDevice));
                    Logger.LogInfo("C");
                    window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(() => { closePluginWindow(); });
                    connectButton.onClick.AddListener(() => { connectToMidiDevice(); });
                    disconnectButton.onClick.AddListener(() => { DisconnectFromMidiDevice(); });
                    copyNoteOnButton.onClick.AddListener(() => { copyLastNoteOnTrigger(); });
                    copyNoteOffButton.onClick.AddListener(() => { copyLastNoteOffTrigger(); });
                    copyControlChangeButton.onClick.AddListener(() => { copyLastControlChangeTrigger(); });
                }
                catch (Exception e)
                {
                    Logger.LogError($"Couldn't initialize MIDI Input List: {e.Message}");
                    setStatusTitle("Couldn't initialize MIDI Input List");
                    setStatusMessage($"initError: {e.Message}\n{e.StackTrace}");
                    Logger.LogError(e.ToString());
                }

                try
                {
                    connectToMidiDevice();
                }
                catch (Exception e)
                {
                    Logger.LogError($"Couldn't initialize MIDI Input Connection: {e.Message}");
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

        private List<TMP_Dropdown.OptionData> buildInputOptionsList()
        {
            Logger.LogInfo($"Checking for Input Devices...");
            List<InputDevice> inputDevices = listInputDevices();
            List<TMP_Dropdown.OptionData> optionsList = [];

            inputDeviceNames = [];
            
            inputDeviceNames.Add("<None>");
            optionsList.Add(new TMP_Dropdown.OptionData("<None>"));

            foreach (InputDevice device in inputDevices)
            {
                inputDeviceNames.Add(device.Name);
                optionsList.Add(new TMP_Dropdown.OptionData(device.Name));
            }

            return optionsList;
        }

        private List<TMP_Dropdown.OptionData> buildOutputOptionsList()
        {
            List<OutputDevice> outputDevices = listOutputDevices();
            List<TMP_Dropdown.OptionData> optionsList = [];

            outputDeviceNames = [];
            
            outputDeviceNames.Add("<None>");
            optionsList.Add(new TMP_Dropdown.OptionData("<None>"));

            foreach (OutputDevice device in outputDevices)
            {
                outputDeviceNames.Add(device.Name);
                optionsList.Add(new TMP_Dropdown.OptionData(device.Name));
            }

            return optionsList;
        }


        private void loadPluginSettings()
        {
            // Get settings in dictionary
            Dictionary<string, string> settings = VNyanInterface.VNyanInterface.VNyanSettings.loadSettings("JayoMidiPlugin.cfg");
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

            VNyanInterface.VNyanInterface.VNyanSettings.saveSettings("JayoMidiPlugin.cfg", settings);
        }

        public void pluginButtonClicked()
        {
            // Flip the visibility of the window when plugin window button is clicked
            if (window != null)
            {
                window.SetActive(!window.activeSelf);
                if(window.activeSelf)
                    window.transform.SetAsLastSibling();
            }
                
        }

        private void setStatusTitle(string titleText)
        {
            statusText.text = titleText;
        }

        private void setStatusMessage(string messageText)
        {
            errorText.text = messageText;
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
            Logger.LogInfo($"Initializing MIDI Connection");
            if(midiInputDevice == "<None>" || midiInputDevice == "" || midiInputDevice == null)
            {
                setStatusTitle("Select a MIDI Device");
                return;
            }

            setStatusTitle("Initializing MIDI Connection");
            if (GetComponent<MidiManager>().initMidi(midiInputDevice))
            {
                connectButton.gameObject.SetActive(false);
                disconnectButton.gameObject.SetActive(true);
                inputSelectDropdown.interactable = false;
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
                connectButton.gameObject.SetActive(true);
                disconnectButton.gameObject.SetActive(false);
                inputSelectDropdown.interactable = true;
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

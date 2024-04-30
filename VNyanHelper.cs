using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using VNyanInterface;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace JayoMidiPlugin
{
    public static class VNyanHelper
    {
        public static void setVNyanParameterFloat(string parameterName, float value)
        {
            Debug.Log($"Setting parameter { parameterName } to {value.ToString()}");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat(parameterName, value);
            } else
            {
                findTestHarness().setFloatParameter(parameterName, value);
            }
        }

        public static void setVNyanParameterString(string parameterName, string value)
        {
            Debug.Log($"Setting parameter { parameterName } to {value}");
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(parameterName, value);
            } else
            {
                findTestHarness().setStringParameter(parameterName, value);
            }
        }

        public static float getVNyanParameterFloat(string parameterName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat(parameterName);
            } else
            {
                return findTestHarness().getFloatParameter(parameterName);
            }
        }

        public static string getVNyanParameterString(string parameterName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanParameter == null))
            {
                return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterString(parameterName);
            } else
            {
                return findTestHarness().getStringParameter(parameterName);
            }
        }

        public static GameObject pluginSetup(IButtonClickedHandler pluginInstance, string buttonText, GameObject windowPrefab)
        {
            // Register button to plugins window
            if (!(VNyanInterface.VNyanInterface.VNyanUI == null))
            {
                VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton(buttonText, pluginInstance);
                Debug.Log($"Instantiating Window Prefab");
                // Create a window that will show when the button in plugins window is clicked
                return (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
            } else {
                Debug.Log($"(Test Mode) Instantiating Window Prefab");
                GameObject window = GameObject.Instantiate(windowPrefab);
                window.transform.parent = findTestCanvasObject().transform;
                return window;
            }
                
            
        }

        public static Dictionary<string, string> loadPluginSettingsData(string fileName)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanSettings == null))
            {
                return VNyanInterface.VNyanInterface.VNyanSettings.loadSettings(fileName);
            } else {
                return findTestHarness().loadPluginSettingsData(fileName);
            }
        }

        public static void savePluginSettingsData(string fileName, Dictionary<string, string> pluginSettingsData)
        {
            if (!(VNyanInterface.VNyanInterface.VNyanSettings == null))
            {
                VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(fileName, pluginSettingsData);
            } else {
                findTestHarness().savePluginSettingsData(fileName, pluginSettingsData);
            }
        }

        private static VNyanTestHarness findTestHarness()
        {
            var foundHarnessObject = GameObject.Find("__VNyanTestHarness");
            if (foundHarnessObject != null)
            {
                Debug.Log($"Found Test Harness");
                return foundHarnessObject.GetComponent<VNyanTestHarness>();
            }
            else
            {
                Debug.Log($"Instantiating Test Harness");
                GameObject harnessObject = new GameObject("__VNyanTestHarness");
                harnessObject.AddComponent<VNyanTestHarness>();
                return harnessObject.GetComponent<VNyanTestHarness>();
            }
        }

        private static GameObject findTestCanvasObject()
        {
            var foundCanvasObject = GameObject.Find("__VNyanTestCanvas");
            if (foundCanvasObject != null)
            {
                Debug.Log($"Found Test Canvas");
                return foundCanvasObject;
            }
            else
            {
                Debug.Log($"Instantiating Test Canvas");
                GameObject canvasObject = new GameObject("__VNyanTestCanvas");
                canvasObject.AddComponent<Canvas>();
                Canvas myCanvas = canvasObject.GetComponent<Canvas>();
                myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                return canvasObject;
            }
        }
    }
}

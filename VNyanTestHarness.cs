using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JayoMidiPlugin
{
    internal class VNyanTestHarness : MonoBehaviour
    {
        private Dictionary<string, Dictionary<string, string>> pluginSettingsData = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> VNyanStringParameters = new Dictionary<string, string>();
        private Dictionary<string, float> VNyanFloatParameters = new Dictionary<string, float>();

        public void savePluginSettingsData(string fileName, Dictionary<string, string> newPluginSettingsData)
        {
            pluginSettingsData[fileName] = newPluginSettingsData;
        }

        public Dictionary<string, string> loadPluginSettingsData(string fileName)
        {
            Dictionary<string, string> loadedPluginSettingsData;
            if (pluginSettingsData.TryGetValue(fileName, out loadedPluginSettingsData))
            {
                return loadedPluginSettingsData;
            }
            return new Dictionary<string, string>();
        }

        public void setStringParameter(string parameterName, string value)
        {
            VNyanStringParameters[parameterName] = value;
        }

        public string getStringParameter(string parameterName)
        {
            string loadedParameter;
            if(VNyanStringParameters.TryGetValue(parameterName, out loadedParameter))
            { 
                return loadedParameter;
            }
            return "";
        }

        public void setFloatParameter(string parameterName, float value)
        {
            VNyanFloatParameters[parameterName] = value;
        }

        public float getFloatParameter(string parameterName)
        {
            float loadedParameter;
            if(VNyanFloatParameters.TryGetValue(parameterName, out loadedParameter))
            { 
                return loadedParameter;
            }
            return 0.0f;
        }
    }
}

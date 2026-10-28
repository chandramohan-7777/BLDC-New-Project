using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BLDC_Demo.Models
{
    public static class MotorParamConfig
    {
        public static List<MotorParameterDefinition> LeftParameters { get; private set; }
            = new List<MotorParameterDefinition>();

        public static List<MotorParameterDefinition> RightParameters { get; private set; }
            = new List<MotorParameterDefinition>();

        private class MotorParamFile
        {
            public List<MotorParameterDefinition> Left { get; set; }
            public List<MotorParameterDefinition> Right { get; set; }
        }

        public static void LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.WriteLine($"MotorParamConfig: file not found at '{path}'.");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<MotorParamFile>(json, options);

                LeftParameters = data?.Left ?? new List<MotorParameterDefinition>();
                RightParameters = data?.Right ?? new List<MotorParameterDefinition>();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"MotorParamConfig: failed to load '{path}': {ex.Message}");
            }
        }
    }
}
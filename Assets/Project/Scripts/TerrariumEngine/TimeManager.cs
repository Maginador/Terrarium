using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrariumEngine
{
    /// <summary>
    /// Manages time flow in the terrarium system with acceleration capabilities
    /// </summary>
    public class TimeManager : MonoBehaviour, IDebuggable
    {
        [Header("Time Settings")]
        [SerializeField] private float[] timeSpeeds = { 1f, 2f, 4f, 10f };
        [SerializeField] private int defaultTimeSpeedIndex = 0;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugUI = true;
        
        public string DebugName => "TimeManager";
        public bool IsDebugEnabled { get; set; } = true;
        
        private int _currentTimeSpeedIndex = 0;
        private float _currentTimeScale = 1f;
        
        public float CurrentTimeScale => _currentTimeScale;
        public int CurrentTimeSpeedIndex => _currentTimeSpeedIndex;
        public float[] AvailableTimeSpeeds => timeSpeeds;
        
        public System.Action<float> OnTimeScaleChanged;
        
        private void Awake()
        {
            _currentTimeSpeedIndex = defaultTimeSpeedIndex;
            _currentTimeScale = timeSpeeds[_currentTimeSpeedIndex];
            Time.timeScale = _currentTimeScale;
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Set the time speed by index
        /// </summary>
        /// <param name="speedIndex">Index in the timeSpeeds array</param>
        public void SetTimeSpeed(int speedIndex)
        {
            if (speedIndex < 0 || speedIndex >= timeSpeeds.Length)
            {
                Debug.LogWarning($"TimeManager: Invalid speed index {speedIndex}. Must be between 0 and {timeSpeeds.Length - 1}");
                return;
            }
            
            _currentTimeSpeedIndex = speedIndex;
            _currentTimeScale = timeSpeeds[speedIndex];
            Time.timeScale = _currentTimeScale;
            
            OnTimeScaleChanged?.Invoke(_currentTimeScale);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"TimeManager: Time scale changed to {_currentTimeScale}x");
            }
        }
        
        /// <summary>
        /// Cycle to the next time speed
        /// </summary>
        public void NextTimeSpeed()
        {
            int nextIndex = (_currentTimeSpeedIndex + 1) % timeSpeeds.Length;
            SetTimeSpeed(nextIndex);
        }
        
        /// <summary>
        /// Cycle to the previous time speed
        /// </summary>
        public void PreviousTimeSpeed()
        {
            int prevIndex = (_currentTimeSpeedIndex - 1 + timeSpeeds.Length) % timeSpeeds.Length;
            SetTimeSpeed(prevIndex);
        }
        
        /// <summary>
        /// Reset time to normal speed (1x)
        /// </summary>
        public void ResetTimeSpeed()
        {
            SetTimeSpeed(0);
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugUI = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugUI || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Time Controls", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label($"Current Speed: {_currentTimeScale}x", GUI.skin.label);
            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x")) SetTimeSpeed(0);
            if (GUILayout.Button("2x")) SetTimeSpeed(1);
            if (GUILayout.Button("4x")) SetTimeSpeed(2);
            if (GUILayout.Button("10x")) SetTimeSpeed(3);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<< Previous")) PreviousTimeSpeed();
            if (GUILayout.Button("Next >>")) NextTimeSpeed();
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

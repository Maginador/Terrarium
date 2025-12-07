using System.Collections.Generic;
using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Global debug manager that handles all debug elements in the terrarium system
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugByDefault = true;
        
        private static DebugManager _instance;
        public static DebugManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DebugManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DebugManager");
                        _instance = go.AddComponent<DebugManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private Dictionary<string, IDebuggable> _debuggableObjects = new Dictionary<string, IDebuggable>();
        private bool _globalDebugEnabled = true;
        
        public bool GlobalDebugEnabled
        {
            get => _globalDebugEnabled;
            set
            {
                _globalDebugEnabled = value;
                UpdateAllDebugStates();
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                _globalDebugEnabled = enableDebugByDefault;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Register a debuggable object with the debug manager
        /// </summary>
        /// <param name="debuggable">The debuggable object to register</param>
        public void RegisterDebuggable(IDebuggable debuggable)
        {
            if (debuggable == null) return;
            
            string key = debuggable.DebugName;
            if (_debuggableObjects.ContainsKey(key))
            {
                Debug.LogWarning($"DebugManager: Object with name '{key}' is already registered. Overwriting.");
            }
            
            _debuggableObjects[key] = debuggable;
            debuggable.IsDebugEnabled = _globalDebugEnabled;
            debuggable.OnDebugStateChanged(_globalDebugEnabled);
        }
        
        /// <summary>
        /// Unregister a debuggable object from the debug manager
        /// </summary>
        /// <param name="debuggable">The debuggable object to unregister</param>
        public void UnregisterDebuggable(IDebuggable debuggable)
        {
            if (debuggable == null) return;
            
            string key = debuggable.DebugName;
            if (_debuggableObjects.ContainsKey(key))
            {
                _debuggableObjects.Remove(key);
            }
        }
        
        /// <summary>
        /// Enable or disable a specific debug element
        /// </summary>
        /// <param name="debugName">Name of the debug element</param>
        /// <param name="enabled">Whether to enable or disable</param>
        public void SetDebugElementEnabled(string debugName, bool enabled)
        {
            if (_debuggableObjects.TryGetValue(debugName, out IDebuggable debuggable))
            {
                debuggable.IsDebugEnabled = enabled && _globalDebugEnabled;
                debuggable.OnDebugStateChanged(debuggable.IsDebugEnabled);
            }
        }
        
        /// <summary>
        /// Get the current state of a debug element
        /// </summary>
        /// <param name="debugName">Name of the debug element</param>
        /// <returns>True if enabled, false otherwise</returns>
        public bool IsDebugElementEnabled(string debugName)
        {
            if (_debuggableObjects.TryGetValue(debugName, out IDebuggable debuggable))
            {
                return debuggable.IsDebugEnabled;
            }
            return false;
        }
        
        /// <summary>
        /// Get all registered debug element names
        /// </summary>
        /// <returns>Array of debug element names</returns>
        public string[] GetRegisteredDebugElements()
        {
            string[] names = new string[_debuggableObjects.Count];
            _debuggableObjects.Keys.CopyTo(names, 0);
            return names;
        }
        
        private void UpdateAllDebugStates()
        {
            foreach (var kvp in _debuggableObjects)
            {
                kvp.Value.IsDebugEnabled = _globalDebugEnabled;
                kvp.Value.OnDebugStateChanged(_globalDebugEnabled);
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}


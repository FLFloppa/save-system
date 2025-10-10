using System;
using Cysharp.Threading.Tasks;
using FLFloppa.SaveSystem;
using UnityEngine;

namespace FLFloppa.SaveSystem.Samples
{
    public sealed class SampleSaveBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SaveServiceConfiguration saveServiceConfiguration;
        [Header("Sample Payload")]
        [SerializeField] private SamplePlayerProgress defaultProgress = new SamplePlayerProgress
        {
            levelIndex = 1,
            coins = 25,
            difficulty = "Normal"
        };

        private ISaveService _service;
        private const string SaveKey = "sample_progress";

        private void Awake()
        {
            if (saveServiceConfiguration == null)
            {
                throw new InvalidOperationException("SaveServiceConfiguration reference is missing.");
            }

            _service = saveServiceConfiguration.Build();
            _service.SetProfile("SamplePlayer");
            _service.SetCategory("Demo");
        }

        [ContextMenu("Save Progress")]
        public void Save()
        {
            _service.Save(SaveKey, defaultProgress);
            Debug.Log("Sample progress saved.");
        }

        [ContextMenu("Load Progress")]
        public void Load()
        {
            try
            {
                var envelope = _service.Load<SamplePlayerProgress>(SaveKey);
                Debug.Log($"Loaded progress: {envelope.Data.ToReadableString()}");
            }
            catch (SaveSystemException ex)
            {
                Debug.LogWarning($"Failed to load progress: {ex.Message}");
            }
        }

        [ContextMenu("Save Progress Async")]
        public async void SaveAsync()
        {
            await _service.SaveAsync(SaveKey, defaultProgress);
            Debug.Log("Sample progress saved asynchronously.");
        }

        [ContextMenu("Load Progress Async")]
        public async void LoadAsync()
        {
            try
            {
                var envelope = await _service.LoadAsync<SamplePlayerProgress>(SaveKey);
                Debug.Log($"Loaded progress (async): {envelope.Data.ToReadableString()}");
            }
            catch (SaveSystemException ex)
            {
                Debug.LogWarning($"Failed to load progress asynchronously: {ex.Message}");
            }
        }
    }
}

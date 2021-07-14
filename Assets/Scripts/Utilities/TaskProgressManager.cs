using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Simulator
{
    public class TaskProgressManager : MonoBehaviour
    {
        public Action OnUpdate = delegate { };
        public Action<IProgressTask, bool, Exception> OnTaskCompleted = delegate { };
        public static TaskProgressManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("TaskProgressManager");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<TaskProgressManager>();
                }
                return instance;
            }
        }

        bool updated = false;
        static TaskProgressManager instance;
        public List<IProgressTask> Tasks { get; private set; } = new List<IProgressTask>();

        public void AddTask(IProgressTask task)
        {
            Debug.Log("new task " + task.Description);
            Tasks.Add(task);
            task.OnUpdated += TaskUpdated;
            task.OnCompleted += TaskCompleted;
            updated = true;
        }

        private void TaskUpdated(IProgressTask task)
        {
            updated = true;
        }

        void TaskCompleted(IProgressTask task, bool success, Exception exception)
        {
            Debug.Log($"task completed: {task.Description}: {Mathf.Floor(task.Progress * 100.0f)} success? {success}");
            OnTaskCompleted.Invoke(task, success, exception);
            Tasks.Remove(task);
            task.OnUpdated -= TaskUpdated;
            task.OnCompleted -= TaskCompleted;
            updated = true;
        }

        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (updated)
            {
                updated = false;
                OnUpdate.Invoke();
            }
        }
    }

    public interface IProgressTask
    {
        event Action<IProgressTask> OnUpdated;
        event Action<IProgressTask, bool, Exception> OnCompleted;
        public float Progress { get; }
        string Description { get; }
    }
}
using ReefEditor.UI;
using System.Collections.Generic;

namespace ReefEditor {
    public class LoadingManager {

        private Queue<ILoader> upcomingLoaders;
        private ILoader activeTask;
        public event System.Action OnQueueEmpty;

        public LoadingManager() {
            upcomingLoaders = new Queue<ILoader>();
        }

        public void UpdateLoading() {
            if (activeTask != null) {
                EditorUI.UpdateStatusBar(activeTask.GetTaskDescription(), activeTask.GetTaskProgress());
                if (activeTask.IsFinished()) {
                    EditorUI.DisableStatusBar();

                    if (upcomingLoaders.Count > 0) {
                        activeTask = upcomingLoaders.Dequeue();
                        activeTask.StartLoading();
                    } else {
                        OnQueueEmpty?.Invoke();
                        activeTask = null;
                        OnQueueEmpty = null;
                    }
                }
            }
        }

        public void AddLoader(ILoader loader) {
            if (activeTask != null) {
                upcomingLoaders.Enqueue(loader);
            } else {
                activeTask = loader;
                activeTask.StartLoading();
            }
        }
    }
    
    public interface ILoader {
        void StartLoading();
        bool IsFinished();
        float GetTaskProgress();
        string GetTaskDescription();
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDbImportTool
{
    internal static class TaskHelper
    {
        internal static void RunAsyncTasks(params Action[] tasks)
        {
            var factory = new TaskFactory();
            var startedTasks = tasks.Select(factory.StartNew);
            foreach (var task in startedTasks)
            {
                if (!(task.IsCompleted || task.IsFaulted))
                {
                    task.Wait();
                }
            }
        }
    }
}

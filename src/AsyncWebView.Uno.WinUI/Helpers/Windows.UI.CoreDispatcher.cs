using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Windows.UI.Dispatching
{
	/// <summary>
	/// Extension methods for classes in the Windows.UI.Core namespace.
	/// </summary>
	internal static class DispatcherQueueExtensions
	{
		/// <summary>
		/// This method allows for executing an async Task on the DispatcherQueue.
		/// </summary>
		/// <param name="dispatcherQueue"></param>
		/// <param name="priority"></param>
		/// <param name="asyncAction">The async operation.</param>
		internal static async Task RunTaskAsync(this DispatcherQueue dispatcherQueue, DispatcherQueuePriority priority, Func<Task> asyncAction)
		{
			var completion = new TaskCompletionSource<bool>();
			await dispatcherQueue.RunAsync(priority, RunActionUI);
			await completion.Task;

			async void RunActionUI()
			{
				try
				{
					await asyncAction();
					completion.SetResult(true);
				}
				catch (Exception exception)
				{
					completion.SetException(exception);
				}
			}
		}

		/// <summary>
		/// This method allows for executing an async Task with result on the DispatcherQueue.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="dispatcherQueue"></param>
		/// <param name="priority"></param>
		/// <param name="asyncFunc">The async operation.</param>
		internal static async Task<TResult> RunTaskAsync<TResult>(this DispatcherQueue dispatcherQueue, DispatcherQueuePriority priority, Func<Task<TResult>> asyncFunc)
		{
			var completion = new TaskCompletionSource<TResult>();
			await dispatcherQueue.RunAsync(priority, RunActionUI);
			return await completion.Task;

			async void RunActionUI()
			{
				try
				{
					var result = await asyncFunc();
					completion.SetResult(result);
				}
				catch (Exception exception)
				{
					completion.SetException(exception);
				}
			}
		}
	}
}

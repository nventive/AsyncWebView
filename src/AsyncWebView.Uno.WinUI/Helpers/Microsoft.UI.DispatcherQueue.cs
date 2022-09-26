using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Dispatching
{
	/// <summary>
	/// This class exposes extensions methods on <see cref="DispatcherQueue"/>.
	/// </summary>
	internal static class DispatcherQueueExtensions
	{
		/// <summary>
		/// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
		/// <see cref="Task"/> that completes when the invocation of the function is completed.
		/// </summary>
		/// <param name="taskCompletionSource">The <see cref="TaskCompletionSource"/> that will set the result or Exception depending on the result of the invocation</param>
		/// <param name="handler">The <see cref="DispatcherQueueHandler"/> to invoke.</param>
		/// <returns>A <see cref="DispatcherQueueHandler"/> that completes when the invocation of <paramref name="handler"/> is over.</returns>
		internal static DispatcherQueueHandler InvokeHandler(DispatcherQueueHandler handler, TaskCompletionSource<object?> taskCompletionSource)
		{
			try
			{
				handler.Invoke();

				taskCompletionSource.SetResult(null);
			}
			catch (Exception e)
			{
				taskCompletionSource.SetException(e);
			}

			return handler;
		}

		/// <summary>
		/// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
		/// <see cref="Task"/> that completes when the invocation of the function is completed.
		/// </summary>
		/// <param name="dispatcher">The target <see cref="DispatcherQueue"/> to invoke the code on.</param>
		/// <param name="handler">The <see cref="DispatcherQueueHandler"/> to invoke.</param>
		/// <param name="priority">The priority level for the function to invoke.</param>
		/// <returns>A <see cref="Task"/> that completes when the invocation of <paramref name="handler"/> is over.</returns>
		/// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="handler"/> will be invoked directly.</remarks>
		internal static Task RunAsync(this DispatcherQueue dispatcher, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, DispatcherQueueHandler handler = default)
		{
			// Run the function directly when we have thread access.
			// Also reuse Task.CompletedTask in case of success,
			// to skip an unnecessary heap allocation for every invocation.
			if (dispatcher.HasThreadAccess)
			{
				try
				{
					handler.Invoke();

					return Task.CompletedTask;
				}
				catch (Exception e)
				{
					return Task.FromException(e);
				}
			}

			return TryEnqueueAsync(dispatcher, handler, priority);
		}

		/// <summary>
		/// Invokes a given function on the target <see cref="DispatcherQueue"/> and returns a
		/// <see cref="Task"/> that completes when the invocation of the function is completed.
		/// </summary>
		/// <param name="dispatcher">The target <see cref="DispatcherQueue"/> to invoke the code on.</param>
		/// <param name="asyncAction">The <see cref="DispatcherQueueHandler"/> to invoke.</param>
		/// <param name="priority">The priority level for the function to invoke.</param>
		/// <returns>A <see cref="Task"/> that completes when the invocation of <paramref name="asyncAction"/> is over.</returns>
		/// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="asyncAction"/> will be invoked directly.</remarks>
		internal static Task<T> RunTaskAsync<T>(this DispatcherQueue dispatcher, DispatcherQueuePriority priority, Func<Task<T>> asyncAction)
		{
			if (dispatcher.HasThreadAccess)
			{
				try
				{
					if (asyncAction() is Task<T> awaitableResult)
					{
						return awaitableResult;
					}

					return Task.FromException<T>(GetEnqueueException("The Task returned by function cannot be null."));
				}
				catch (Exception e)
				{
					return Task.FromException<T>(e);
				}
			}

			static Task<T> TryEnqueueAsync(DispatcherQueue dispatcher, Func<Task<T>> function, DispatcherQueuePriority priority)
			{
				var taskCompletionSource = new TaskCompletionSource<T>();

				if (!dispatcher.TryEnqueue(priority, async () =>
				{
					try
					{
						if (function() is Task<T> awaitableResult)
						{
							var result = await awaitableResult.ConfigureAwait(false);

							taskCompletionSource.SetResult(result);
						}
						else
						{
							taskCompletionSource.SetException(GetEnqueueException("The Task returned by function cannot be null."));
						}
					}
					catch (Exception e)
					{
						taskCompletionSource.SetException(e);
					}
				}))
				{
					taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
				}

				return taskCompletionSource.Task;
			}

			return TryEnqueueAsync(dispatcher, asyncAction, priority);
		}

		internal static Task TryEnqueueAsync(DispatcherQueue dispatcher, DispatcherQueueHandler handler, DispatcherQueuePriority priority)
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			DispatcherQueueHandler callback = InvokeHandler(handler, taskCompletionSource);

			if (!dispatcher.TryEnqueue(priority, callback))
			{
				taskCompletionSource.SetException(GetEnqueueException("Failed to enqueue the operation"));
			}

			return taskCompletionSource.Task;
		}

		/// <summary>
		/// Creates an <see cref="InvalidOperationException"/> to return when an enqueue operation fails.
		/// </summary>
		/// <param name="message">The message of the exception.</param>
		/// <returns>An <see cref="InvalidOperationException"/> with a specified message.</returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static InvalidOperationException GetEnqueueException(string message)
		{
			return new InvalidOperationException(message);
		}
	}
}

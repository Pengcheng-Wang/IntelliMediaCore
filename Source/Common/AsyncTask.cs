//---------------------------------------------------------------------------------------
// Copyright 2015 North Carolina State University
//
// Center for Educational Informatics
// http://www.cei.ncsu.edu/
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
//   * Redistributions of source code must retain the above copyright notice, this 
//     list of conditions and the following disclaimer.
//   * Redistributions in binary form must reproduce the above copyright notice, 
//     this list of conditions and the following disclaimer in the documentation 
//     and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
// GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//---------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace IntelliMedia
{
	public delegate void CompletedHandler(object result);
	public delegate void ActionHandler(AsyncTask previousTask, CompletedHandler onComplete, ErrorHandler onError);
	public delegate void ErrorHandler(Exception e);
	public delegate void FinallyHandler();

	public class AsyncTask
	{
		private ActionHandler onAction;
		private CompletedHandler onCompleted;
		private ErrorHandler onError;
		private FinallyHandler onFinally;

		private AsyncTask parent;
		private List<AsyncTask> nextTasks = new List<AsyncTask>();

		public object Result { get; private set; }

		public AsyncTask(ActionHandler actionHandler)
		{
			onAction = actionHandler;
		}			

		public T ResultAs<T>()
		{
			if (Result is T)
			{
				return (T)Result;
			}
			else
			{
				throw new Exception(String.Format("Attempting to cast AsyncTask result '{0}' to '{1}'", 
					(Result != null ? Result.GetType().Name : "null"),
					typeof(T).Name));				
			}
		}

		public AsyncTask Then(ActionHandler actionHandler)
		{
			AsyncTask next = new AsyncTask(actionHandler);
			next.parent = this;
			nextTasks.Add(next);

			return next;
		}

		public AsyncTask Catch(ErrorHandler errorHandler)
		{
			if (errorHandler != null) 
			{
				onError += errorHandler;
			}

			return this;
		}

		public AsyncTask Finally(FinallyHandler action)
		{
			if (action != null) 
			{
				onFinally += action;
			}

			return this;
		}

		public void Start(CompletedHandler completedHandler = null, ErrorHandler errorHandler = null)
		{
			AsyncTask rootTask = this;
			while (rootTask.parent != null) 
			{
				rootTask = rootTask.parent;
			}

			if (rootTask == this) 
			{
				rootTask.ExecuteTask (null, completedHandler, errorHandler);
			}
			else 
			{
				if (completedHandler != null) {
					this.onCompleted += completedHandler;
				}
				if (errorHandler != null) {
					this.onError += errorHandler;
				}
				rootTask.ExecuteTask (null, null, null);
			}
		}

		private void ExecuteTask(AsyncTask prevResult = null, CompletedHandler completedHandler = null, ErrorHandler errorHandler = null)
		{
			if (completedHandler != null) 
			{
				onCompleted += completedHandler;
			}

			if (errorHandler != null) 
			{
				onError += errorHandler;
			}

			try
			{
				onAction(prevResult, Completed, Error);
			}
			catch(Exception e) 
			{
				Error(e);
			}
		}

		private void Completed(object result)
		{
			if (onCompleted != null) 
			{
				onCompleted(result);
			}

			Result = result;
			if (Result is AsyncTask) 
			{
				nextTasks.Insert(0, (AsyncTask)Result);
			}

			foreach (AsyncTask task in nextTasks) 
			{
				task.ExecuteTask(this, null, null);
			}

			if (onFinally != null) 
			{
				onFinally();
			}
		}

		private void Error(Exception e)
		{
			if (onError != null) 
			{
				onError (e);
			}

			foreach (AsyncTask task in nextTasks) 
			{				
				task.Error(e);
			}	

			if (onFinally != null) 
			{
				onFinally();
			}
		}

		public class AsyncTaskQueue
		{
			Queue<AsyncTask> taskQueue;

			public AsyncTaskQueue(IEnumerable<AsyncTask> tasks)
			{
				taskQueue = new Queue<AsyncTask>(tasks);
			}
		}

		public static AsyncTask Until(IEnumerable<AsyncTask> tasks, Func<object, bool> condition)
		{
			return new AsyncTask((prevTask, onCompleted, onError) =>
			{
				AsyncTaskQueue queue = new AsyncTaskQueue(tasks);
			});
		}
	}
}


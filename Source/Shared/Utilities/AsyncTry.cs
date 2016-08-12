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

namespace IntelliMedia.Utilities
{
	public delegate IAsyncTask HandlerReturnsAsync<TResult>(TResult result);
	public delegate object HandlerReturnsResult<TResult>(TResult result);
	public delegate void HandlerReturnsVoid<TResult>(TResult result);
	public delegate IAsyncTask TaskHandler(object result);

	public class AsyncTry : IAsyncTask
	{		
		private CompletedHandler onCompleted;
		private ErrorHandler onError;	
		private Action onFinally;
		private List<IAsyncTask> tasks = new List<IAsyncTask>();
		private List<TaskHandler> handlers = new List<TaskHandler>();
		private int currentTaskIndex;

		public AsyncTry(IAsyncTask task)
		{
			Contract.ArgumentNotNull("task", task);

			tasks.Add(task);
		}	
			
		public AsyncTry Then<T>(HandlerReturnsAsync<T> taskHandler)
		{
			Contract.ArgumentNotNull("taskHandler", taskHandler);

			handlers.Add((result) => taskHandler((T)result));

			return this;
		}

		public AsyncTry Then<T>(HandlerReturnsResult<T> taskHandler)
		{
			Contract.ArgumentNotNull("taskHandler", taskHandler);

			handlers.Add((result) => AsyncTask.WithResult(taskHandler((T)result)));

			return this;
		}	

		public AsyncTry Then<T>(HandlerReturnsVoid<T> taskHandler)
		{
			Contract.ArgumentNotNull("taskHandler", taskHandler);

			handlers.Add((result) =>
			{
				taskHandler((T)result);
				return AsyncTask.WithResult("void");
			});

			return this;
		}

		public AsyncTry Catch(ErrorHandler errorHandler)
		{
			Contract.ArgumentNotNull("errorHandler", errorHandler);

			onError += errorHandler;

			return this;
		}

		public AsyncTry Finally(Action finallyHandler)
		{
			Contract.ArgumentNotNull("finallyHandler", finallyHandler);

			onFinally += finallyHandler;

			return this;
		}

		#region IAsyncTask implementation

		public object Result { get { return tasks[tasks.Count - 1].Result; }}

		public void Start(CompletedHandler completedHandler = null, ErrorHandler errorHandler = null)
		{
			if (completedHandler != null)
			{
				onCompleted += completedHandler;
			}

			if (errorHandler != null)
			{
				onError += errorHandler;
			}

			currentTaskIndex = 0;
			ExecuteNextTask();
		}

		#endregion

		private void ExecuteNextTask()
		{
			if (currentTaskIndex >= tasks.Count)
			{
				Completed();
				return;
			}

			try
			{
				int taskIndex = currentTaskIndex++;
				tasks[taskIndex].Start((result) =>
				{
					if (taskIndex < handlers.Count)
					{
						tasks.Add(handlers[taskIndex](result));
					}
					ExecuteNextTask();
				}, 
				Error);	
			}
			catch(Exception e)
			{
				Error(e);
			}
		}

		private void Completed()
		{
			if (onCompleted != null)
			{
				onCompleted(Result);
			}

			Finished();
		}
				
		private void Error(Exception e)
		{
			if (onError != null)
			{
				onError(e);
				Finished();
			}
			else
			{
				Finished();
				throw e;
			}					
		}

		private void Finished()
		{
			if (onFinally != null)
			{
				onFinally();
			}
		}
	}
}


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
	public delegate bool AsyncResultCondition(object result);

	public class AsyncForEach : IAsyncTask
	{
		private CompletedHandler onCompleted;
		private ErrorHandler onError;
		private IEnumerable<IAsyncTask> tasks;
		private IEnumerator<IAsyncTask> taskEnumerator;
		private AsyncResultCondition breakCondition;

		public AsyncForEach(IEnumerable<IAsyncTask> tasks, AsyncResultCondition breakCondition)
		{
			Contract.ArgumentNotNull("tasks", tasks);
			Contract.ArgumentNotNull("breakCondition", breakCondition);

			this.tasks = tasks;
			this.breakCondition = breakCondition;
		}				

		#region IAsyncTask implementation

		public void Start(CompletedHandler completedHandler = null, ErrorHandler errorHandler = null)
		{
			this.onCompleted = completedHandler;
			this.onError = errorHandler;
			this.taskEnumerator = tasks.GetEnumerator();
			StartNext();
		}

		public object Result { get; private set; }

		#endregion

		private void StartNext()
		{
			try
			{
				if (taskEnumerator != null)
				{
					if (taskEnumerator.MoveNext())
					{
						taskEnumerator.Current.Start((result) =>
						{
							if (breakCondition(result))
							{
								Completed(result);
								return;
							}
							StartNext();
						},
						Error);
					}
					else
					{
						taskEnumerator.Dispose();
						taskEnumerator = null;
						Completed(tasks);
					}
				}
				else
				{
					throw new Exception("Attmpeting to start task after iterating through all tasks.");
				}
			}
			catch(Exception e)
			{
				Error(e);
			}
		}

		private void Completed(object result)
		{
			Result = result;
			if (onCompleted != null)
			{
				onCompleted(Result);
			}
		}

		private void Error(Exception e)
		{
			if (onError != null)
			{
				onError(e);
			}
			else
			{
				throw e;
			}	
		}
	}
}
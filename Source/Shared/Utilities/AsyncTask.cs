﻿//---------------------------------------------------------------------------------------
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
	public class AsyncTask : IAsyncTask
	{
		public delegate void TaskHandler(CompletedHandler onCompleted, ErrorHandler onError = null);
		protected TaskHandler task;

		public object Result { get; private set; }

		public AsyncTask(TaskHandler taskHandler)
		{
			Contract.ArgumentNotNull("taskHandler", taskHandler);
			task = taskHandler;
		}	

		public static AsyncTask WithResult(object result)
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(result);
			});
		}				

		public virtual void Start(CompletedHandler completedHandler = null, ErrorHandler errorHandler = null)
		{
			try
			{
				task((result) =>
				{
					Result = result;
					if (completedHandler != null)
					{
						completedHandler(result);
					}
				}, 
				(ex) =>
				{
					HandleError(errorHandler, ex);
				});
			}
			catch(Exception e)
			{
				HandleError(errorHandler, e);
			}	
		}

		protected static void HandleError(ErrorHandler errorHandler, Exception e)
		{
			if (errorHandler != null)
			{
				errorHandler(e);
			}
			else
			{
				throw e;
			}			
		}			
	}
}


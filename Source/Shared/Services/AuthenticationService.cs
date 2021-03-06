﻿//---------------------------------------------------------------------------------------
// Copyright 2014 North Carolina State University
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
using System.Collections;
using IntelliMedia.Models;
using IntelliMedia.Repositories;
using IntelliMedia.Utilities;

namespace IntelliMedia.Services
{
	public class AuthenticationService 
	{
		private AppSettings appSettings;

		public string Token { get; private set; }

		public delegate void AuthenticateHandler(bool success, string message);

		public AuthenticationService(AppSettings appSettings)
		{
			this.appSettings = appSettings;
		}
		
		public IAsyncTask SignIn(string domain, string username, string password)
		{
			Uri serverUri = new Uri(appSettings.ServerURI, UriKind.RelativeOrAbsolute);
			Uri restUri = new Uri(serverUri, "rest/");

			StudentRepository repo = new StudentRepository(restUri);
			if (repo == null)
			{
				throw new Exception("StudentRepository is not initialized.");
			}
			
			return new AsyncTry(repo.SignIn(domain, username, password))
				.Then<Student>((student) =>
				{
					Token = Guid.NewGuid().ToString();
					return student;
				});                   
		}
		
		public AsyncTask SignOut()
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				// TODO Release token
				Token = null;
				onCompleted(true);
			});
		}
	}
}

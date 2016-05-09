//---------------------------------------------------------------------------------------
// Copyright 2016 North Carolina State University
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
using UnityEngine;
using Zenject;

namespace IntelliMedia
{
	/// <summary>
	/// Configure repository settings during app startup
	/// </summary>	
	public class RepositoryConfigurator : MonoBehaviour 
	{
		public string serverUrl = "http://intellimedia-portal.appspot.com/";

		// After Dependency Injection
		[PostInject]
		public void Init(AppSettings appSettings)
		{
			// TODO rgtaylor 2016-04-03 Added a keyboard shortcut to switch server
			appSettings.ServerURI = serverUrl;
			//appSettings.ServerURI = "http://intellimedia-portal.appspot.com/";
			//appSettings.ServerURI = "http://intellimedia-portal-dev.appspot.com/";
			//appSettings.ServerURI = "http://localhost:8888/";
		}			
	}
}
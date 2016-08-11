//---------------------------------------------------------------------------------------
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
using System.Text.RegularExpressions;


#if !(SILVERLIGHT || WPF || TOOL)
using UnityEngine;
using HttpWebResponse = IntelliMedia.Utilities.UnityWebResponse;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace IntelliMedia.Utilities
{
    /// <summary>
    /// This class simplifies network access by executing HTTP commands and processing 
	/// the response string.
    /// </summary>
    public class HttpClient
    {
        /// <summary>
        /// Post a single MIME encoded payload to a URI
        /// </summary>
        /// <param name="uri">The URI of the web service to receive the Http post message</param>
        /// <param name="mimePart">MIME encoded payload</param>
        /// <returns>Async task return returns the string result of the HTTP command</returns>
		public AsyncTask<string> Post(Uri uri, MimePart mimePart)
        {
            return Post(uri, false, new List<MimePart> { mimePart });
        }

        /// <summary>
        /// Post one or more MIME parts to a URI
        /// </summary>
        /// <param name="uri">The URI of the web service to receive the Http post message</param>
        /// <param name="mimeParts">MIME encoded payload</param>
		/// <returns>Async task return returns the string result of the HTTP command</returns>
		public AsyncTask<string> Post(Uri uri, bool forceMultipart, List<MimePart> mimeParts)
        {
			return new AsyncTask<string>((onCompleted, onError) =>
			{			
#if (SILVERLIGHT || WPF || TOOL)
	            WebRequest webRequest = WebRequest.Create(uri);
#else
	            WebRequest webRequest = new UnityWebRequest(uri);
#endif
	            webRequest.Method = "POST";
	            webRequest.BeginGetRequestStream((asynchronousResult) =>
	            {
					try
					{					
		                WebRequest request = (WebRequest)asynchronousResult.AsyncState;

						if (mimeParts.Count > 1 || forceMultipart)
		                {
		                    CreateMultiPartRequest(request, asynchronousResult, mimeParts);
		                }
		                else
		                {
		                    CreateSinglePartRequest(request, asynchronousResult, mimeParts[0]);
		                }

		                // Start the asynchronous operation to get the response
		                request.BeginGetResponse((responseResult) =>
		                {
		                    try
		                    {
		                        HttpWebResponse response = ((WebRequest)responseResult.AsyncState).EndGetResponse(responseResult) as HttpWebResponse;
		                        // Response stream is released when HttpResult is released
								using (HttpResult httpResult = new HttpResult(response.GetResponseStream(), response.StatusCode, response.StatusDescription, response.ContentType))
								{
									ProcessResult(httpResult, onCompleted);
								}
		                    }
		                    catch (Exception e)
		                    {
		                        DebugLog.Error("HTTP {0} '{1}' failed: {2}", webRequest.Method, uri.ToString(), e.Message);
		                        onError(e);
		                    }
		                },
		                request);
					}
					catch (Exception e)
					{
						DebugLog.Error("HTTP {0} '{1}' failed: {2}", webRequest.Method, uri.ToString(), e.Message);
						onError(e);
					}						
	            },
                webRequest);
			});
		}

        /// <summary>
        /// Get response stream from a web service indicated by URI 
        /// </summary>
        /// <param name="uri">URI (path plus query string) to web service</param>
		/// <returns>Async task return returns the string result of the HTTP command</returns>
		public AsyncTask<string> Get(Uri uri)
        {
			return new AsyncTask<string>((onCompleted, onError) =>
			{				
#if (SILVERLIGHT || WPF || TOOL)
	            WebRequest webRequest = WebRequest.Create(uri);
#else
	            WebRequest webRequest = new UnityWebRequest(uri);
#endif
	            webRequest.Method = "GET";
	            webRequest.BeginGetResponse((responseResult) =>
	            {
	                try
	                {
	                    HttpWebResponse response = ((WebRequest)responseResult.AsyncState).EndGetResponse(responseResult) as HttpWebResponse;
	                    // Response stream is released when HttpResult is released
						using (HttpResult httpResult = new HttpResult(response.GetResponseStream(), response.StatusCode, response.StatusDescription, response.ContentType))
						{
							ProcessResult(httpResult, onCompleted);
						}
	                }
	                catch (Exception e)
	                {
						DebugLog.Error("HTTP {0} '{1}' failed: {2}", webRequest.Method, uri.ToString(), e.Message);
						onError(e);
	                }
	            },
	            webRequest);
			});
		}

        private static void CreateSinglePartRequest(WebRequest request, IAsyncResult asyncResult, MimePart part)
        {
            // End the operation
            using (Stream postStream = request.EndGetRequestStream(asyncResult))
            {
                if (!part.IsFile)
                {
                    request.ContentType = string.Format("{0}; charset=UTF-8", part.ContentType);

                    // Write text value into the HTTP request
                    byte[] byteArray = Encoding.UTF8.GetBytes(part.Value);
                    postStream.Write(byteArray, 0, byteArray.Length);
                }
                else
                {
                    request.ContentType = part.ContentType;
                    // Copy binary data to HTTP request
                    part.Stream.CopyTo(postStream);
                }
            }
        }

        private static void CreateMultiPartRequest(WebRequest request, IAsyncResult asyncResult, List<MimePart> parts)
        {
            string boundary = DateTime.Now.Ticks.ToString("x");
            request.ContentType = "multipart/form-data; boundary=" + boundary;

            using (Stream postStream = request.EndGetRequestStream(asyncResult))
            {
                foreach (MimePart part in parts)
                {
                    if (part.IsFile)
                    {
                        StringBuilder sbHeader = new StringBuilder();
                        sbHeader.AppendFormat("--{0}", boundary);
                        sbHeader.Append("\r\n");
						sbHeader.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", part.Name, part.Filename);
                        sbHeader.Append("\r\n");
                        sbHeader.AppendFormat("Content-Type: {0}", part.ContentType);
                        sbHeader.Append("\r\n");
                        sbHeader.Append("\r\n");
                        byte[] header = Encoding.UTF8.GetBytes(sbHeader.ToString());
                        postStream.Write(header, 0, header.Length);
                        part.Stream.CopyTo(postStream);
                        byte[] crlf = Encoding.UTF8.GetBytes("\r\n");
                        postStream.Write(crlf, 0, crlf.Length);
                    }
                    else
                    {
                        StringBuilder sbHeader = new StringBuilder();
                        sbHeader.AppendFormat("--{0}", boundary);
                        sbHeader.Append("\r\n");
						sbHeader.AppendFormat("Content-Disposition: form-data; name=\"{0}\"", part.Name);
                        sbHeader.Append("\r\n");
                        sbHeader.AppendFormat("Content-Type: {0}; charset=UTF-8", part.ContentType);
                        sbHeader.Append("\r\n");
                        sbHeader.Append("\r\n");
                        sbHeader.Append(part.Value);
                        sbHeader.Append("\r\n");

                        byte[] header = Encoding.UTF8.GetBytes(sbHeader.ToString());
                        postStream.Write(header, 0, header.Length);
                    }
                }

                byte[] footer = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");
                postStream.Write(footer, 0, footer.Length);
            }
        }

		private void ProcessResult(HttpResult httpResult, CompletedHandler onCompleted)
		{
			// Check server response
			CheckServerResult(httpResult);

			// Process response before using block ends and releases resources
			onCompleted(httpResult.Response);
		}

		private static readonly string httpErrorRegexPattern = @"^(?<status>\d*)\s*(?<message>.+)";

		private void CheckServerResult(HttpResult httpResult)
		{       
			// Parse HTTP exception message
			if (httpResult.Error != null && !string.IsNullOrEmpty(httpResult.Error.Message))
			{
				Regex errorRegex = new Regex(httpErrorRegexPattern, RegexOptions.None);
				MatchCollection matches = errorRegex.Matches(httpResult.Error.Message);
				if (matches.Count > 0 && matches[0].Success)
				{
					throw new Exception(matches[0].Groups["message"].Value);
				}
			}

			// Accept any HTTP status code in the 2xx range as success
			else if (httpResult.StatusCode != System.Net.HttpStatusCode.OK
				&& httpResult.StatusCode != System.Net.HttpStatusCode.Created
				&& httpResult.StatusCode != System.Net.HttpStatusCode.Accepted
				&& httpResult.StatusCode != System.Net.HttpStatusCode.NonAuthoritativeInformation
				&& httpResult.StatusCode != System.Net.HttpStatusCode.NoContent
				&& httpResult.StatusCode != System.Net.HttpStatusCode.ResetContent
				&& httpResult.StatusCode != System.Net.HttpStatusCode.PartialContent)
			{
				// If this fails, confirm all "correct" status codes are listed above AND
				// check the parsing code in UnityWebResponse.
				throw new Exception(string.Format("Unexpected Status Code: {0}", httpResult.StatusDescription));
			}
		}
    }
}

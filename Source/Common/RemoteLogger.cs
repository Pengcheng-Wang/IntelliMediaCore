//---------------------------------------------------------------------------------------
// Copyright 2016 North Carolina State University
//
// Computer Science Department
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
using System.Text;
using System.Collections.Generic;

namespace IntelliMedia
{
	public class RemoteLogger : ILogger
	{   
		public string LogName { get; private set; }
        public Uri ServerUrl { get; private set; }

        private System.IO.StreamWriter LogStreamChunk { get; set; }
        private ISerializer Serializer { get; set; }
		private HttpClient httpClient;
		private const int DefaultRetryCount = 1;

		public RemoteLogger(string logName, string serverUrl, ISerializer serializer, bool enabled = true)
        {
			Contract.ArgumentNotNull("serverUrl", serverUrl);
            Contract.ArgumentNotNull("serializer", serializer);

			LogName = logName;
			ServerUrl = new Uri(serverUrl, UriKind.RelativeOrAbsolute);
            Serializer = serializer;

			httpClient = new HttpClient();

            Enabled = enabled;
        }

        #region ILogger implementation

        private bool enabled;
        public bool Enabled
        {
            get { return enabled; } 
            set { if (enabled != value) { enabled = value; OnEnabledChanged(); } }
        }
        
        private void OnEnabledChanged()
        {
            DebugLog.Info("RemoteLogger: {0} Enabled={1}", ServerUrl, Enabled); 

            // Lazy initialization so that this can be enabled/disabled after a session has started
            if (Enabled && LogStreamChunk == null)
            {
				Contract.PropertyNotNull("ServerUrl", ServerUrl);
               
				LogStreamChunk = new System.IO.StreamWriter(new System.IO.MemoryStream());
                LogStreamChunk.Write(Serializer.GetHeader<LogEntry>());
            }
        }

        public void Write(LogEntry entry)
        {
            if (Enabled)
            {
                LogStreamChunk.Write(Serializer.Serialize(entry));
            }
        }

		public void Close(LoggerCloseCallback callback)
        {
            if (LogStreamChunk != null)
            {
                LogStreamChunk.Flush();

				UploadFile(LogStreamChunk.BaseStream, LogName + ".csv", (success, error) =>
				{
					LogStreamChunk.Dispose();
					LogStreamChunk = null;
					ServerUrl = null;
					LogName = null;

					callback(success, error);
				});
			}
        }
        
        #endregion

		private delegate void UploadFileHandler(bool success, string error);

		private void UploadFile(System.IO.Stream stream, string filename, UploadFileHandler callback)
		{	
			stream.Seek(0, System.IO.SeekOrigin.Begin);

			MimePart chunkInfo = new MimePart("MyPart", MimePart.UrlEncoded, "Finished", "true");
			MimePart mimePart = new MimePart("fileUpload", MimePart.TextCsv, filename, stream);
			
			httpClient.Post(ServerUrl, DefaultRetryCount, true, new List<MimePart>() { mimePart, chunkInfo }, (postResult) =>
			{
				string error = null;
				try
				{
					using (HttpResult httpResult = postResult.AsyncState as HttpResult)
					{
						// Check server response
						if (httpResult.StatusCode == System.Net.HttpStatusCode.OK)
						{
							// Success!
						}
						else
						{
							throw new Exception(string.Format("Server responded with {0}. {1}", 
							                                  httpResult.StatusCode, 
							                                  httpResult.StatusDescription));
						}
					}
				}
				catch (Exception e)
				{
					error = "Unable to upload trace data to the cloud. " + e.Message;
				}

				if (callback != null)
				{
					callback(error != null, error);
				}

				if (!String.IsNullOrEmpty(error))
				{
					DebugLog.Error(error);
				}
			});
		}
    }
}


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
using System.Linq;

namespace IntelliMedia
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ViewDescriptorAttribute : Attribute
	{
		public Type ViewModelType { get; private set; }
		public string[] Capabilities { get; private set; }

		public ViewDescriptorAttribute(Type viewModelType, params string[] capabilities)
		{
			this.ViewModelType = viewModelType;
			this.Capabilities = capabilities;
		}

		public static bool HasCapabilities(string[] required, string[] viewCapabilities)
		{
			if (required == null || required.Length == 0)
			{
				return true;
			}

			if (viewCapabilities == null || viewCapabilities.Length == 0)
			{
				return false;
			}

			return !required.Except(viewCapabilities).Any();
		}

		public static ViewDescriptorAttribute FindOn(Type type)
		{
			object[] attributes = type.GetCustomAttributes(typeof(ViewDescriptorAttribute), true);
			if (attributes == null || attributes.Length == 0)
			{
				return null;
			}

			if (attributes.Length > 1)
			{
				throw new Exception(string.Format("Multiple {0} attached to '{1}'",
					typeof(ViewDescriptorAttribute), type.Name));
			}

			return attributes[0] as ViewDescriptorAttribute;
		}
	}
}


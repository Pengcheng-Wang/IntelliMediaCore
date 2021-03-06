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
using System.Collections.Generic;
using IntelliMedia.Utilities;

namespace IntelliMedia.DecisionMaking
{
    public class SelectDestination : BehaviorTask
	{
        readonly static string DestinationNameKey = "DestinationName";
        public string[] DestinationGameObjectNames { set; get; }
		int index;

		public SelectDestination(params string[] destinationNames)
		{
			DestinationGameObjectNames = destinationNames;
		}

        public static void SetDestinationName(Blackboard knowledge, string destinationName)
        {
            knowledge.Set(DestinationNameKey, destinationName);
        }
        
        public static string GetDestinationName(Blackboard knowledge)
        {
            return knowledge.Get<string>(DestinationNameKey);
        }

        public override IEnumerable<CooperativeTaskStatus> DoWork()
		{
			if (DestinationGameObjectNames != null && DestinationGameObjectNames.Length > 0)
			{
                SetDestinationName(Context.LocalKnowledge, DestinationGameObjectNames[index]);
				index = (index + 1)%DestinationGameObjectNames.Length; 
                yield return Finished(true);
			}

            yield return Finished(false);
		}
	}
}

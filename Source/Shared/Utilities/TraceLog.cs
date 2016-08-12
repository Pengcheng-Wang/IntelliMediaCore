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
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using IntelliMedia.Models;

namespace IntelliMedia.Utilities
{
    /// <summary>
    /// This provides static methods for recording trace data.
    /// </summary>
    public class TraceLog
    {
        public enum Action
        {
            /// <summary>
            /// Denotes that an object that can be opened has been opened (e.g., Book, 
            /// GUI, ScanningDevice, door, or jar).
            /// </summary>
            Opened,
            /// <summary>
            /// Denotes that an object has been closed (e.g., Book, GUI, ScanningDevice, 
            /// door, or jar). The duration of how long the object was open will be logged.
            /// </summary>
            Closed,
            /// <summary>
            /// Denotes that something that has an on/off state has been turned on (e.g.,
            /// AI agent, light switch)
            /// </summary>
            Enabled,
            /// <summary>
            /// Denotes that something that has been turned off (e.g.,
            /// AI agent, light switch). The duration of how long the object was enabled 
            /// will be logged.
            /// </summary>
            Disabled,
            /// <summary>
            /// Denotes that an option from a list of 2 or more things has been chosen (e.g.,
            /// scanning device setting, answer on concept matrix, or diagnosis worksheet 
            /// entry). NOTE: Prefer enabled/disable if the nature of the interaction is off/on.
            /// </summary>
            Selected,
            /// <summary>
            /// Denotes that an option from a list of 2 or more things has been un-chosen and
            /// returned to the default state. (e.g., scanning device option, answer on 
            /// concept matrix, or diagnosis worksheet entry).
            /// </summary>
            Deselected,
            /// <summary>
            /// Indicates that a connection has been established (e.g., a network connection)
            /// </summary>
            ConnectedTo,
            /// <summary>
            /// A previously established connection has been disconnected. The duration of the 
            /// connection will be logged.
            /// </summary>
            DisconnectedFrom,
            /// <summary>
            /// Denote the start of general action or process. NOTE: Use 
            /// Open/Closed, Activated/Deactivated, or Selected/Deselected for 
            /// more specific actions.
            /// </summary>
            Started,
            /// <summary>
            /// Denote the end of general action that has a duration. Use 
            /// Open/Closed, Activated/Deactivated, or Selected/Deselected for 
            /// more specific actions.
            /// </summary>
            Ended,
			/// <summary>
			/// A long running action has been suspended.
			/// </summary>
			Paused,
			/// <summary>
			/// Something is shown or highlighted.
			/// </summary>
			Revealed,
			/// <summary>
			/// The player has moved to a new named location in the scene (e.g. Infirmary)
			/// </summary>
			Resumed,
            /// <summary>
            /// The player has moved to a new named location in the scene (e.g. Infirmary)
            /// </summary>
            MovedTo,
            /// <summary>
            /// The player has moved from a named location in the scene (e.g. Lab). The
            /// duration at the location will be logged.
            /// </summary>
            MovedFrom,
            /// <summary>
            /// The actor has aquired an object and is now carrying it.
            /// </summary>
            PickedUp,
            /// <summary>
            /// The actor has released an object it was previously carrying.
            /// </summary>
            Dropped,
            /// <summary>
            /// Uses eye tracking to indicate that the player has looked at a 3D 
            /// object or GUI.
            /// </summary>
            LookedAt,
            /// <summary>
            /// Indicates when a player speaks to an NPC or an NPC speaks to a player.
            /// This denotes face-to-face interaction.
            /// </summary>
            SpokeTo,
            /// <summary>
            /// Denotes that a one-shot activation has occurred. This is for logging actions
            /// that are instantaneous and do not have state. (E.g., teleporting, indicating
            /// a plot point has been achieved)
            /// </summary>
            Activated,
			/// <summary>
			/// Denotes that a sensor has been calibrated.
			/// </summary>
			Calibrated,
            /// <summary>
            /// This action is used to indicate that *content* has been 'viewed' by an actor.
            /// However, whether the actor actually understood or read the content is unknown.
            /// For example: text in virtual book or an assessment. NOTE: use Open/Closed to
            /// indicate GUIs being displayed. This action should be used to indicate the 
            /// nature of information on the GUI or in the 3D world.
            /// </summary>
            ViewedContentOf,
            /// <summary>
            /// Indicates that a one-shot message has been sent or an indicator has been 
            /// displayed (e.g., message from a tutor, a message box)
            /// </summary>
            Alerted,
            /// <summary>
            /// Indicates that someting has had a one-shot check made. For example, grading
            /// a player's answer to a question or an agent determining the next course of
            /// action to take.
            /// </summary>
            Evaluated,
            /// <summary>
            /// Indicates that an actor has changed the state of an object. (e.g., the player
            /// or NPC causes a state machine to transition to a new state).
            /// </summary>
            ChangedStateOf,
			/// <summary>
			/// A data sample has been recorded that is periodic in nature. For example, 
			/// biometric data such as eye tracking or GSR. 
			/// </summary>
			Sampled,
			/// <summary>
			/// An action or task has been completed or finished. 
			/// </summary>
			Completed,
			/// <summary>
			/// A player answer or response has been recorded 
			/// </summary>
			Submitted,
        }

        public delegate void UpdateLogEntry(LogEntry entry);

        public static string SessionId { get; private set; }
        public static int SequenceNumber { get; private set; }
        public static readonly List<ILogger> Loggers = new List<ILogger>();
        public static readonly List<UpdateLogEntry> LogEntryModifiers = new List<UpdateLogEntry>();

        public static void Open(string sessionId, params ILogger[] initialLoggers)
        {                   
            SessionId = sessionId;
            SequenceNumber = 0;
            Loggers.AddRange(initialLoggers);
        }

        public static void SetLoggerEnabled<T>(bool enabled) where T : ILogger
        {
            foreach (ILogger logger in TraceLog.Loggers)
            {
                if (logger is T)
                {
                    logger.Enabled = enabled;
                }
            }
        }
		        
		public static void Close(LoggerCloseCallback callback)
        {
            try
            {   
				ILogger[] loggersToClose = Loggers.ToArray();
				Loggers.Clear();

				CallbackAccumulator accumulator = new CallbackAccumulator(loggersToClose.Length, callback);
				foreach (ILogger logger in loggersToClose)
                {
					logger.Close(accumulator.Callback);
                }
            }
            catch(Exception e)
            {
				callback(false, string.Format("Failed to close all trace logs. {0}", e.Message));
            }
        }

		private class CallbackAccumulator
		{
			private readonly object syncLock = new object();
			private int CallbacksExpected;
			private LoggerCloseCallback CloseCallback { get; set; }
			private StringBuilder ErrorMsg { get; set; }

			public CallbackAccumulator(int callbacksExpected, LoggerCloseCallback callback)
			{
				Contract.Argument("Must be expecting at least one callback", "callbacksExpected", callbacksExpected > 0);
				Contract.ArgumentNotNull("callback", callback);

				ErrorMsg = new StringBuilder();

				CallbacksExpected = callbacksExpected;
				CloseCallback = callback;
			}

			public void Callback(bool success, string error)
			{
				lock(syncLock)
				{
					--CallbacksExpected;
					if (error != null)
					{
						ErrorMsg.AppendFormat("{0}. ",  error);
					}
				}

				if (CallbacksExpected < 1)
				{
					bool allSuccess = ErrorMsg.Length == 0; 
					CloseCallback(allSuccess, (allSuccess ? null : ErrorMsg.ToString()));
				}
			}
		}

        public static LogEntry Player(Action action, string target, params object[] attributes)
        {
            return Write("Player", action, target, attributes);
        }

        public static LogEntry Player(LogEntry startEntry, Action action, string target, params object[] attributes)
        {
            return Write(startEntry, "Player", action, target, attributes);
        }

        public static LogEntry Write(string actor, Action action, string target, params object[] attributes)
        {
            return Write(action,
                  "Actor", actor,
                  "Target", target,
                  attributes);
        }

        public static LogEntry Write(LogEntry startEntry, string actor, Action action, string target, params object[] attributes)
        {
            if (startEntry != null)
            {
                return Write(action,
                             "Actor", actor,
                             "Target", target,
                             "Duration", (DateTime.Now - startEntry.TimeStamp).TotalSeconds,
                             attributes);
            }
            else
            {
                return Write(action,
                             "Actor", actor,
                             "Target", target,
                             attributes);
            }
        }

        public static LogEntry Write(Action action, params object[] attributes)
        {
            LogEntry entry = new LogEntry(SessionId, SequenceNumber++, action.ToString(), attributes);
            foreach (UpdateLogEntry modifier in LogEntryModifiers)
            {
                modifier(entry);
            }
            
            foreach (ILogger logger in Loggers)
            {
                logger.Write(entry);
            }

            return entry;
        }
    }
}


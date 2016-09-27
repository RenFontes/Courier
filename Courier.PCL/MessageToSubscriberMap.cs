using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CourierB
{
	[DebuggerDisplay("Subscriber Count: {SubscriberCount}")]
	internal class MessageToSubscriberMap : IDisposable
	{
		//Store mappings with weak references to prevent leaks
		private readonly Dictionary<MessageToken, WeakSubscriber> Map = new Dictionary<MessageToken, WeakSubscriber>();

		internal void AddSubscriber(MessageToken token, Object callbackTarget, MethodInfo callbackMethod, 
			Object onErrorTarget, MethodInfo onErrorMethod, Type subscriberType)
		{
			if (token == null)
				throw new ArgumentNullException("token");

			if (callbackMethod == null)
				throw new ArgumentNullException("callbackMethod");

			lock (Map)
			{
				if (!Map.ContainsKey(token))
				{
					Map.Add(token, new WeakSubscriber(callbackTarget, callbackMethod, onErrorTarget, onErrorMethod, subscriberType));
				}
				else
				{
					Map[token] = new WeakSubscriber(callbackTarget, callbackMethod, onErrorTarget, onErrorMethod, subscriberType);
				}
			}
		}

		internal void RemoveSubscriber(MessageToken token)
		{
			lock (Map)
			{
				if (Map.ContainsKey(token))
				{
					Map.Remove(token);
				}
			}
		}

		internal IEnumerable<MulticastDelegate> GetSubscribers<T>(String message)
		{
			if (String.IsNullOrEmpty(message))
				throw new ArgumentNullException("message");

			List<MulticastDelegate> subscribers;
			lock (Map)
			{
				List<WeakSubscriber> weakSubscribers = Map.Where(subscriber => subscriber.Key.Message == message).Select(subscriber => subscriber.Value).ToList();

				subscribers = new List<MulticastDelegate>(weakSubscribers.Count());

				foreach (var weakSubscriber in weakSubscribers)
				{
					MulticastDelegate weakSub = weakSubscriber.CreateCallbackDelegate();
					if (weakSub != null)
					{
						subscribers.Add(weakSub);
					}
				}
			}
			return subscribers;
		}

		private static Type GetMethodArgumentType(WeakSubscriber subscriber)
		{

		    var argType = subscriber.ParameterType ?? typeof(object);
			return argType;
		}

		private static bool VerifyParameterType<T>(WeakSubscriber subscriber)
		{
			//Make sure that the callbackTarget delegate has the same type of T as the boadcaster is sending
			Type argType = GetMethodArgumentType(subscriber);
			return argType == typeof(T);
		}

		internal bool IsSubscribed(MessageToken messageToken)
		{
			return Map.ContainsKey(messageToken);
		}

		#region IDisposable Members
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~MessageToSubscriberMap()
		{
			Dispose();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Map != null)
				{
					Map.Clear();
				}
			}
		}
		#endregion

		#region Debugger Support
		
		private Int32 SubscriberCount
		{
			get
			{
				return Map.Count();
			}
		}
		
		#endregion
	}
}
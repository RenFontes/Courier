using System;
using System.Collections.Generic;
using System.Linq;


namespace Courier
{
	/// <summary>
	/// The base mediator class that handles subscriptions, message dispatching and message caching
	/// </summary>
	public class Mediator : IDisposable
	{
		#region private fields
		private readonly MessageToSubscriberMap _subscribers = new MessageToSubscriberMap();
		private readonly List<CachedMessage> _cachedMessages = new List<CachedMessage>();
		#endregion

		#region private methods
		private void InternalBroadcastMessage<T>(string message, T parameter)
		{
		    var parameterType = parameter != null ? parameter.GetType() : null;

            //Get subscribers that can receive the parameter, and void subscribers. Or void subscribers if the parameter is null
		    var subscriberList =
		        _subscribers.GetSubscribers<T>(message)
		            .Where(
		                s =>
		                    s.GetType().IsGenericType
		                        ? s.GetType().GetGenericArguments()[0].IsAssignableFrom(parameterType) ||
		                          s.GetType() == typeof (Action)
		                        : s.GetType() == typeof (Action))
		            .ToList();

		    foreach (var subscriber in subscriberList)
		    {
		        //if there is no parameter or subscriber has no parameters, invoke without parameters
		        if (parameter == null || subscriber.GetType() == typeof(Action))
		        {
		            subscriber.DynamicInvoke();
		        }
		        else
		        {
		            subscriber.DynamicInvoke(parameter);
		        }
		    }


		    SendMessageBroadcastEvent(message, parameter);
		}

		private void BroadcastCachedMessages(string message, MulticastDelegate callback)
		{
			CleanOutCache();
			//Search the cache for matches messages
			var matches = _cachedMessages.FindAllSL(action => action.Message == message);
			//If we find matches invoke the delegate passed in and pass the message payload
			foreach (var cachedMessage in matches)
			{
				if (callback.Method.GetParameters().Length == 0)
				{
					callback.DynamicInvoke();
				}
				else
				{
					callback.DynamicInvoke(cachedMessage.Parameter);
				}
				cachedMessage.ResendCount++;
			}
			CleanOutCache();
		}

		private MessageToken InternalRegisterForMessage(MulticastDelegate callback, MulticastDelegate onError, string message, bool excludeCachedMessages)
		{
		    var parameterType = callback.GetType().IsGenericType ? callback.GetType().GetGenericArguments()[0] : null;

			if (callback.Target == null)
				throw new InvalidOperationException("Delegate cannot be static");

			if (!excludeCachedMessages)
			{
				BroadcastCachedMessages(message, callback);
			}

			var token = MessageToken.GenerateToken(message);

			if (onError != null)
			{
				_subscribers.AddSubscriber(token, callback.Target, callback.Method, onError.Target, onError.Method, parameterType);
			}
			else
			{
				_subscribers.AddSubscriber(token, callback.Target, callback.Method, null, null, parameterType);
			}

			return token;
		}

		private void CleanOutCache()
		{
			//Remove any expired messages from the cache
			_cachedMessages.RemoveAllSL(message => (message.CacheOptions.ExpirationDate < DateTime.Now) || (message.ResendCount >= message.CacheOptions.NumberOfResends));
		}
		#endregion

		#region internal event

		internal event EventHandler<MessageBroadcastArgs> MessageBroadcastEvent;
		internal event EventHandler<MessageBroadcastArgs> MessageBroadcast
		{
			add { MessageBroadcastEvent += value; }
			remove { MessageBroadcastEvent -= value; }
		}

		internal void SendMessageBroadcastEvent<T>(String message, T payload)
		{
			var args = new MessageBroadcastArgs(message, payload);
            MessageBroadcastEvent?.Invoke(this, args);
        }


		#endregion

		#region public methods

		#region register methods
		/// <summary>
		/// Register for the message by Type T. No name is given so one will be generated internally
		/// </summary>
		/// <param name="callback">The delegate to invoke when the specified message is raised</param>
		/// <returns>a MessageToken to be used to unregister for the message and to verify subscription</returns>
		public MessageToken RegisterForMessage<T>(Action<T> callback)
		{
			Type paramType = typeof(T);
			return InternalRegisterForMessage(callback, null, paramType.FullName, false);
		}

		/// <summary>
		/// Register for the message by Type T. No name is given so one will be generated internally
		/// </summary>
		/// <param name="callback">The delegate to invoke when the specified message is raised</param>
		/// <param name="onError">The delegate to invoke when an error has occured on message dispatch</param>
		/// <returns>a MessageToken to be used to unregister for the message and to verify subscription</returns>
		public MessageToken RegisterForMessage<T>(Action<T> callback, Action<Exception> onError)
		{
			Type paramType = typeof(T);
			return InternalRegisterForMessage(callback, onError, paramType.FullName, false);
		}

		/// <summary>
		/// Register for the specified message. When the specified message is broadcast the subscribers delegate
		/// will be invoked
		/// </summary>
		/// <param name="message">The message to subscribe to</param>
		/// <param name="callback">The delegate to invoke when the specified message is raised</param>
		/// <returns>a MessageToken to be used to unregister for the message and to verify subscription</returns>
		public MessageToken RegisterForMessage<T>(String message, Action<T> callback)
		{
			return InternalRegisterForMessage(callback, null, message, false);
		}

		/// <summary>
		/// Register for the specified message. When the specified message is broadcast the subscribers delegate
		/// will be invoked
		/// </summary>
		/// <param name="message">The message to subscribe to</param>
		/// <param name="callback">The delegate to invoke when the specified message is raised</param>
		/// <param name="onError">The delegate to invoke when an error has occured on message dispatch</param>
		/// <returns>a MessageToken to be used to unregister for the message and to verify subscription</returns>
		public MessageToken RegisterForMessage<T>(String message, Action<T> callback, Action<Exception> onError)
		{
			return InternalRegisterForMessage(callback, onError, message, false);
		}

		/// <summary>
		/// Register for the specified message. When the specified message is broadcast the subscribers delegate
		/// will be invoked
		/// </summary>
		/// <param name="message">The message to subscribe to</param>
		/// <param name="callback">The delegate to invoke when the specified message is raised</param>
		/// <param name="excludeCachedMessages">Opt out of receiving cached messages</param>
		/// <returns>a MessageToken to be used to unregister for the message and to verify subscription</returns>
		public MessageToken RegisterForMessage<T>(String message, Action<T> callback, Boolean excludeCachedMessages)
		{
			return InternalRegisterForMessage(callback, null, message, excludeCachedMessages);
		}

		/// <summary>
		/// Register for the specified message. When the specified message is broadcast the subscribers delegate
		/// will be invoked
		/// </summary>
		/// <param name="message">The message to subscribe to</param>
		/// <param name="callback">The delegate to invoke when the specified message is raised</param>
		/// <param name="onError">The delegate to invoke when an error has occured on message dispatch</param>
		/// <param name="excludeCachedMessages">Opt out of receiving cached messages</param>
		/// <returns>a MessageToken to be used to unregister for the message and to verify subscription</returns>
		public MessageToken RegisterForMessage<T>(String message, Action<T> callback, Action<Exception> onError, Boolean excludeCachedMessages)
		{
			return InternalRegisterForMessage(callback, onError, message, excludeCachedMessages);
		}

		/// <summary>
		/// Register for a message only by name.
		/// </summary>
		/// <param name="message">the message name</param>
		/// <param name="callback">the handler to raise when the message is broadcast</param>
		/// <returns></returns>
		public MessageToken RegisterForMessage(String message, Action callback)
		{
			return InternalRegisterForMessage(callback, null, message, false);
		}

		/// <summary>
		/// Register for a message only by name.
		/// </summary>
		/// <param name="message">the message name</param>
		/// <param name="callback">the handler to raise when the message is broadcast</param>
		/// <param name="onError">The delegate to invoke when an error has occured on message dispatch</param>
		/// <returns></returns>
		public MessageToken RegisterForMessage(String message, Action callback, Action<Exception> onError)
		{
			return InternalRegisterForMessage(callback, onError, message, false);
		}
		#endregion

		/// <summary>
		/// Remove the subscription from the mediator to prevent any future messages from being dispatched to the 
		/// specified tokens endpoint 
		/// </summary>
		/// <param name="token">The token that identifies the subscriber to remove</param>
		public void UnRegisterForMessage(MessageToken token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			_subscribers.RemoveSubscriber(token);
		}

		/// <summary>
		/// Look to see if the specified token is listed as a subscriber
		/// </summary>
		/// <param name="messageToken">The token to check</param>
		public Boolean IsSubscribed(MessageToken messageToken)
		{
			return _subscribers.IsSubscribed(messageToken);
		}

		/// <summary>
		/// Given a message will tell you if there is a message that matches in the cache
		/// </summary>
		/// <param name="message">The message to look for in the cache</param>
		/// <returns>True if there is one ion cache that matches, otherwise false</returns>
		public Boolean IsCached(String message)
		{
			//Make sure to clean out the cache before checking
			CleanOutCache();

			var item = _cachedMessages.FirstOrDefault(action => action.Message == message);
			return item != default(CachedMessage);
		}

		/// <summary>
		/// Given a message token the mediator will remove the specified message from cache regardless of 
		/// what the cache settings are. Use this method when you are caching a message idefintley or if 
		/// you want preempt the normal cache expiration pipline
		/// </summary>
		/// <param name="token">The message token returned from the BroadcastMessage method call</param>
		/// <returns>True if the removal suceeded False if the removal failed (i.e. the message is still in cache)</returns>
		public Boolean RemoveFromCache(MessageToken token)
		{
			try
			{
				_cachedMessages.RemoveAllSL(message => message.Token == token);

				var item = _cachedMessages.FirstOrDefault(action => action.Token == token);
				return item != default(CachedMessage);
			}
			catch (ArgumentNullException)
			{
				return false;
			}
		}

		#region broadcast methods
		/// <summary>
		/// Given a parameter the mediator will broadcast this message to all subscribers that are registered
		/// </summary>
		/// <typeparam name="T">The type of the parameter (payload) being passed</typeparam>
		/// <param name="parameter">the payload of the message</param>
		public void BroadcastMessage<T>(T parameter)
		{
			Type paramType = typeof(T);
			InternalBroadcastMessage(paramType.FullName, parameter);
		}

		/// <summary>
		/// Given a message key, and a parameter the mediator will broadcast this message to all subscribers that are registered
		/// </summary>
		/// <typeparam name="T">The type of the parameter (payload) being passed</typeparam>
		/// <param name="message">The name of the message</param>
		/// <param name="parameter">the payload of the message</param>
		public void BroadcastMessage<T>(String message, T parameter)
		{
			InternalBroadcastMessage(message, parameter);
		}

		/// <summary>
		/// Given a message key, and cache settings the mediator will broadcast this message
		/// to all subscribers that are registered
		/// </summary>
		/// <param name="message">The name of the message</param>
		/// <param name="cacheOptions">The cache settings for this message</param>
		/// <returns>A message token for the cached message. This token can be used to explicitly remove the message from cache</returns>
		public MessageToken BroadcastMessage(String message, CacheSettings cacheOptions)
		{
			return BroadcastMessage<Object>(message, null, cacheOptions);
		}

		/// <summary>
		/// Given a message key, a parameter, and cache settings the mediator will broadcast this message
		/// to all subscribers that are registered
		/// </summary>
		/// <typeparam name="T">The type of the parameter (payload) being passed</typeparam>
		/// <param name="message">The name of the message</param>
		/// <param name="parameter">the payload of the message</param>
		/// <param name="cacheOptions">The cache settings for this message</param>
		/// <returns>A message token for the cached message. This token can be used to explicitly remove the message from cache</returns>
		public MessageToken BroadcastMessage<T>(String message, T parameter, CacheSettings cacheOptions)
		{
			InternalBroadcastMessage(message, parameter);
			//Cache the message for broadcast later
			var cachedMessage = new CachedMessage(message, parameter, cacheOptions);
			_cachedMessages.Add(cachedMessage);
			return cachedMessage.Token;
		}

		/// <summary>
		/// Broadcast a message by name with no callback to invoke. This can be used a simple "ping" type notification where you don't care about a payload
		/// just that the message has been fired.
		/// </summary>
		/// <param name="message"></param>
		public void BroadcastMessage(String message)
		{
			InternalBroadcastMessage<Object>(message, null);
		}
		#endregion
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Dispose of the Mediator
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Explicit Mediator deconstructor
		/// </summary>
		~Mediator()
		{
			Dispose();
		}

		/// <summary>
		/// Dispose of the mediator. Flush the cach and dispose all the subscribers
		/// </summary>
		/// <param name="disposing">True is the object is in the process of being disposed via a call to the Dispose method</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			    _cachedMessages?.Clear();

			    var disposable = _subscribers as IDisposable;

			    disposable?.Dispose();
			}

		}
		#endregion
	}

	/// <summary>
	/// Mediator Factory
	/// </summary>
	public static class MediatorFactory
	{
		private static readonly Mediator InternalMediator = new Mediator();

        /// <summary>
        /// Returns the Mediator Singleton Object
        /// </summary>
        /// <returns></returns>
		public static Mediator GetMediator()
        {
            return InternalMediator;
        }
	}
}
using System;
using System.Diagnostics;
using System.Reflection;

namespace CourierB
{
	[DebuggerDisplay("MethodInfo.Name: {CallbackMethod.Name} WeakReference.Target: {_callbackWeakReference.Target}")]
	internal class WeakSubscriber
	{
	    public MethodInfo CallbackMethod { get; }
	    public MethodInfo OnErrorMethod { get; }
        public Type ParameterType { get; }

	    private readonly Type _delegateType;
		private readonly WeakReference _callbackWeakReference;
		private readonly WeakReference _onErrorWeakReference;

		internal WeakSubscriber(Object callbackTarget, MethodInfo callbackMethod, Object onErrorTarget, MethodInfo onErrorMethod, Type parameterType)
		{
			//create a WeakReference to store the instance of the callbackTarget in which the callbackMethod resides
			_callbackWeakReference = new WeakReference(callbackTarget);
			CallbackMethod = callbackMethod;

			_onErrorWeakReference = new WeakReference(onErrorTarget);
			OnErrorMethod = onErrorMethod;
		    ParameterType = parameterType;

		    _delegateType = parameterType == null ? typeof(Action) : typeof(Action<>).MakeGenericType(parameterType);
		}

		internal MulticastDelegate CreateCallbackDelegate()
		{
            try
            {
                Object target = _callbackWeakReference.Target;
                return target != null ? Delegate.CreateDelegate(_delegateType, _callbackWeakReference.Target, CallbackMethod) as MulticastDelegate : null;

            }
            catch (MemberAccessException)
            {
                return null;
            }
        }

		internal MulticastDelegate CreateOnErrorDelegate()
		{
			try
			{
				var target = _onErrorWeakReference.Target;
				return target != null ? Delegate.CreateDelegate(typeof(Action<Exception>), _onErrorWeakReference.Target, OnErrorMethod) as MulticastDelegate : null;

			}
			catch (MemberAccessException)
			{
				return null;
			}
		}

		public bool IsCallbackAlive => _callbackWeakReference.IsAlive;

	    public bool IsOnErrorAlive => _onErrorWeakReference.IsAlive;
	}
}
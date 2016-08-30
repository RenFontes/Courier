CourierB
=======

This is a fork from courier. Forked it to extend the functionality to support passing different parameters types to the same message.
I also removed some silverlight and wp7 I wasn't going to use.
The documentation is work in progress.

You can find the original Courier in here -> https://github.com/Foovanadil/Courier.

Usage
-----

```C#
//Call the MediatorFactory to get a Mediator Singleton
var mediator = MediatorFactory.GetMediator();

//Register instance method(only instance methods allowed at the moment to message types, the message type is defined by the string. 
//You can include a parameter type. Only one parameter is allowed.
//Message will be received by all compatible listeners. Listeners without parameters will receive parameterized broadcasts without the
//parameters. Listeners where parameter type is the parent type of the broadcast will also receive the message.
mediator.RegisterForMessage("Message", instance.MethodWithoutParameter);
mediator.RegisterForMessage<string>("Message", instance.MethodWithStringParameter);
mediator.RegisterForMessage<object>("Message", instance.MethodWithObjectParameter);

//Send only to non parameterized method
mediator.BroadcastMessage("Message");
//Send to all methods
mediator.BroadcastMessage("Message", "Hello World!");
//Send to non parameterized method and to object method.
mediator.BroadcastMessage("Message", new object());
``` 



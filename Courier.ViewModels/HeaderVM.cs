using System;

namespace CourierB.ViewModels
{
	public class HeaderVM : BaseViewModel
	{
        public String  MessageContent { get; set; }

		public HeaderVM()
		{
			Mediator.RegisterForMessage("Content1Message", (Action<String>)OnContent1MessageReceived);
		}

		private void OnContent1MessageReceived(String messageContent)
		{
			MessageContent = messageContent;
			PropertyChange("MessageContent");
		}
	}
}

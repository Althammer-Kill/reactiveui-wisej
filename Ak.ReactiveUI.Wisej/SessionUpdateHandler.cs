using System;
using System.Collections.Generic;
using System.Diagnostics;
using Wisej.Core;
using Wisej.Web;

namespace ReactiveUI.Wisej
{
	public static class SessionUpdateHandler
	{
		public static HashSet<string> activeUpdates = new HashSet<string>();

		public static void UpdateClientWithLoading(Control control, Action action)
		{
			var id = "";
			Application.RunInContext(control, () =>
			{
				id = Application.SessionId;
			});


			if (activeUpdates.Contains(id))
			{
				//Debug.WriteLine("Skipping nested Update");
				action.Invoke();
				return;
			}

			//Debug.WriteLine("Starting Update");
			activeUpdates.Add(id);
			Application.Update(control, () => control.ShowLoader = true);
			Application.Update(control, action);
			Application.Update(control, () => control.ShowLoader = false);
			activeUpdates.Remove(id);
			//Debug.WriteLine("Ending Update");
		}

		public static void UpdateClient(IWisejComponent context, Action action, bool allowLoader = false)
		{

			if (allowLoader && context is Control control)
			{
				UpdateClientWithLoading(control, action);
				return;
			}

			var id = "";
			Application.RunInContext(context, () =>
			{
				id = Application.SessionId;
			});


			if (activeUpdates.Contains(id))
			{
				//Debug.WriteLine("Skipping nested Update");
				action.Invoke();
				return;
			}

			//Debug.WriteLine("Starting Update");
			activeUpdates.Add(id);
			Application.Update(context, action);
			activeUpdates.Remove(id);
			//Debug.WriteLine("Ending Update");
		}
	}
}

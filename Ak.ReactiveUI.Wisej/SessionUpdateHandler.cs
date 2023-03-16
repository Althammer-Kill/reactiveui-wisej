using System;
using System.Collections.Generic;
using System.Diagnostics;
using Wisej.Core;
using Wisej.Web;

namespace ReactiveUI.Wisej
{
	public static class SessionUpdateHandler
	{
		public static Dictionary<string, int> activeUpdates = new Dictionary<string, int>();

		private static object mutex = new object();


		public static void UpdateClient(IWisejComponent context, Action action, bool allowLoader = true)
		{

			var id = "";
			Application.RunInContext(context, () =>
			{
				id = Application.SessionId;
			});

			var isOuterUpdate = false;

			lock (mutex)
			{
				if (!activeUpdates.ContainsKey(id))
				{
					activeUpdates.Add(id, 1);
					isOuterUpdate = true;
				}
				else
					activeUpdates[id] += 1;
			}

			if (isOuterUpdate && allowLoader && context is Control control)
			{
				control.ShowLoader = true;
				action.Invoke();
				control.ShowLoader = false;
			}
			else
			{
				action.Invoke();
			}

			lock (mutex) { activeUpdates[id] -= 1; }

			bool performUpdate  = false;
			lock (mutex)
			{
				if (activeUpdates[id] <= 0)
				{
					activeUpdates.Remove(id);
					performUpdate = true;
				}
			}

			if(performUpdate)
				Application.Update(context);
		}
	}
}

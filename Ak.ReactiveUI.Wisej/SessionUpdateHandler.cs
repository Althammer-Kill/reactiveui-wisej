using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Wisej.Core;
using Wisej.Web;

namespace ReactiveUI.Wisej
{
	public static class SessionUpdateHandler
	{
		public static void InitializeSession()
		{
			Application.Session.SessionUpdateInfo = new SessionUpdateInfo();
		}


		[ThreadStatic]
		private static bool nestedUpdate;

		public static ConcurrentDictionary<string, object> sessionMutex = new ConcurrentDictionary<string, object>();

		public static void UpdateClient(IWisejComponent context, Action action, bool showLoader = false)
		{
			if (nestedUpdate)
			{
				action.Invoke();
				return;
			}

			SessionUpdateInfo? sessionInfo = null;
			var sessionId = Application.SessionId;
			if (sessionId == null)
			{
				Application.RunInContext(context, () =>
				{
					sessionInfo = Application.Session.SessionUpdateInfo;
				});
			}
			else
			{
				sessionInfo = Application.Session.SessionUpdateInfo;
			}

			RunAction(context, action, showLoader, sessionInfo!);
		}

		private static void RunAction(IWisejComponent context, Action action, bool showLoader, SessionUpdateInfo sessionInfo)
		{
			//lock (sessionInfo)
			{
				try
				{
					if (showLoader && context is Control control)
					{
						if (sessionInfo.LoaderCount == 0)
						{
							Application.Update(context, () => control.ShowLoader = true);
						}

						sessionInfo.LoaderCount++;
					}

					nestedUpdate = true;
					Application.Update(context, action);
				}
				finally
				{
					nestedUpdate = false;
					if (showLoader && context is Control control)
					{
						sessionInfo.LoaderCount--;
						if (sessionInfo.LoaderCount == 0)
						{
							Application.Update(context, () => control.ShowLoader = false);
						}
					}
				}
			}
		}

		private class SessionUpdateInfo
		{
			public int LoaderCount = 0;
		}
	}

	
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Wisej.Core;
using Wisej.Web;

namespace ReactiveUI.Wisej
{
	public static class SessionUpdateHandler
	{
		/// <summary>
		/// The time in milliseconds until the loader is diplayed for any action in <see cref="SessionUpdateHandler.Handle"/>.
		/// </summary>
		private const int LoaderDelay = 300;

		public static Control CurrentPage
		{
			get
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
				return Application.Session.SessionUpdateInfo.CurrentPage;
			}
			set
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
				Application.Session.SessionUpdateInfo.CurrentPage = value;
			}
		}

		public static Form CurrentForm {
			get
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
				return Application.Session.SessionUpdateInfo.CurrentForm;
			}
			set
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
				Application.Session.SessionUpdateInfo.CurrentForm = value;
			}
		}

		public static void UpdateClient(IWisejComponent context, bool forceUpdate = false)
		{
			if (!forceUpdate)
			{
				var sessionInfo = GetSessionInfo(context);
				if (sessionInfo.UpdateInProgress)
					return;
			}
		

			//Debug.WriteLine($"======= UpdateClient {DateTime.Now:HH:mm:ss}");
			Application.Update(context);
		}

		public static void UpdateClient(IWisejComponent context, Action action, bool forceUpdate = false)
		{
			if (!forceUpdate)
			{
				var sessionInfo = GetSessionInfo(context);
				if (sessionInfo.UpdateInProgress)
					return;
			}

			//Debug.WriteLine($"======= UpdateClient {DateTime.Now:HH:mm:ss}");
			Application.Update(context, action);
		}

		/// <summary>
		/// This should only be called in direct EventHandler from Wisej
		/// This method returns immediatly but starts the given action in a background task, that will update the ui when it finished
		/// </summary>
		/// <param name="context"></param>
		/// <exception cref="Exception"></exception>
		public static void Handle(IWisejComponent context, bool showLoader, Func<Task> taskStarter)
		{
			if (Application.SessionId == null)
				throw new InvalidOperationException("We are not in UI Thread");

			Application.StartTask(async () =>
			{
				var sessionInfo = GetSessionInfo(context);

				sessionInfo.UpdateInProgress = true;
				var taskCompleted = false;

				var loaderControl = sessionInfo.CurrentForm ?? sessionInfo.CurrentPage;

				if (showLoader && loaderControl != null)
				{
					var loaderTask = Application.StartTask(async () =>
					{
						await Task.Delay(LoaderDelay);
						if (!taskCompleted)
						{
							loaderControl.ShowLoader = true;
							UpdateClient(context, true);
						}
					});
				}
				
				//Debug.WriteLine($"======= UpdateHandle Start {sessionInfo.LoaderCount}");
				var task = taskStarter();
				await task;
				taskCompleted = true;
				//Debug.WriteLine($"======= UpdateHandle End {sessionInfo.LoaderCount}");

				if(showLoader && loaderControl != null)
					loaderControl.ShowLoader = false;
				UpdateClient(context, true);

				sessionInfo.UpdateInProgress = false;
			});
		}


		public static void Handle(IWisejComponent context, bool showLoader, Action action)
		{
			Handle(context, showLoader, () => Application.StartTask(action));
		}

		public static void Handle(IWisejComponent context, Action action)
		{
			Handle(context, true, action);
		}

		public static void Handle(IWisejComponent context, Func<Task> taskStarter)
		{
			Handle(context, true, taskStarter);
		}

		private class SessionUpdateInfo
		{
			public Form? CurrentForm = null;
			public Control? CurrentPage = null;
			public int LoaderCount = 0;
			public bool UpdateInProgress = false;
		}

		private static SessionUpdateInfo GetSessionInfo(IWisejComponent context)
		{
			SessionUpdateInfo? sessionInfo = null;
			if (Application.SessionId == null)
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

			return sessionInfo ?? new SessionUpdateInfo();
		}
	}

	public class ViewSessionContext
	{
		public CompositeDisposable Disposable { get; protected set; }
		public IScheduler Scheduler { get; protected set; }

		public ViewSessionContext(IScheduler scheduler, CompositeDisposable disposable)
		{
			this.Scheduler = scheduler;
			this.Disposable = disposable;
		}
	}



	
}

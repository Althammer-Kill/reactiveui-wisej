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

		public static void UpdateClient(IWisejComponent context)
		{
			//Debug.WriteLine($"======= UpdateClient");
			Application.Update(context);
		}

		public static void UpdateClient(IWisejComponent context, Action action)
		{
			//Debug.WriteLine($"======= UpdateClient");
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

				var taskCompleted = false;
				var loaderShown = false;

				if (showLoader)
				{
					var loaderTask = Task.Run(async () =>
					{
						await Task.Delay(LoaderDelay);
						if (!taskCompleted)
						{
							ShowLoader(sessionInfo, context, true);
							loaderShown = true;
						}
					});
				}
				
				//Debug.WriteLine($"======= UpdateHandle Start {sessionInfo.LoaderCount}");
				var task = taskStarter();
				await task;
				taskCompleted = true;
				//Debug.WriteLine($"======= UpdateHandle End {sessionInfo.LoaderCount}");

				if (showLoader && loaderShown)
					ShowLoader(sessionInfo, context, false);
				else
					UpdateClient(context);
			});
		}


		public static void Handle(IWisejComponent context, bool showLoader, Action action)
		{
			Handle(context, showLoader, () => Task.Run(action));
		}

		public static void Handle(IWisejComponent context, Action action)
		{
			Handle(context, true, action);
		}

		public static void Handle(IWisejComponent context, Func<Task> taskStarter)
		{
			Handle(context, true, taskStarter);
		}

		private static void ShowLoader(SessionUpdateInfo sessionInfo, IWisejComponent context, bool value)
		{
			lock (sessionInfo)
			{
				sessionInfo.LoaderCount += value ? 1 : -1;
				if (sessionInfo.CurrentForm != null && value)
				{
					sessionInfo.FormStack.Push(sessionInfo.CurrentForm);
					UpdateClient(sessionInfo.CurrentForm, () => sessionInfo.CurrentForm.ShowLoader = true);
				}
				else if  (sessionInfo.FormStack.Count > 0 && !value)
				{
					var form = sessionInfo.FormStack.Pop();
					UpdateClient(form, () => form.ShowLoader = false);
				}
				else if(sessionInfo.CurrentPage != null)
					UpdateClient(sessionInfo.CurrentPage, () => sessionInfo.CurrentPage.ShowLoader = value);
				else if(context is Control control)
					UpdateClient(control, () => control.ShowLoader = value);
				else
					UpdateClient(context);
			}
		}

		private class SessionUpdateInfo
		{
			public Stack<Form> FormStack = new Stack<Form>();
			public Form? CurrentForm = null;
			public Control? CurrentPage = null;
			public int LoaderCount = 0;
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

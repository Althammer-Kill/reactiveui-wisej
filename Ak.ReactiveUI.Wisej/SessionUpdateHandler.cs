﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Web.SessionState;
using Wisej.Core;
using Wisej.Web;

namespace ReactiveUI.Wisej
{
	public static class SessionUpdateHandler
	{
		/// <summary>
		/// The time in milliseconds until the loader is diplayed for any action in <see cref="SessionUpdateHandler.Handle"/>.
		/// </summary>
		private const int LoaderDelay = 200;

		public static Control? CurrentPage
		{
			get
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				return Application.Session.SessionUpdateInfo?.CurrentPage ?? null;
			}
			set
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");
				lock (Application.Session)
				{
					Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
					Application.Session.SessionUpdateInfo.CurrentPage = value;
				}
			}
		}

		public static Form? CurrentForm {
			get
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				return Application.Session?.SessionUpdateInfo?.CurrentForm ?? null;
			}
			set
			{
				if (Application.Session == null)
					throw new InvalidOperationException("Session Object does not exist!");

				lock (Application.Session)
				{
					Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
					Application.Session.SessionUpdateInfo.CurrentForm = value;
				}
			}
		}

		public static void UpdateClient(IWisejComponent context, bool forceUpdate = false)
		{
			if (!forceUpdate)
			{
				var sessionInfo = GetSessionInfo(context);
				if (sessionInfo.IsUpdateInProgress)
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
				if (sessionInfo.IsUpdateInProgress)
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
				 
				sessionInfo.IncrementUpdates();

				var taskCompleted = false;

				var loaderControl = sessionInfo.CurrentForm ?? sessionInfo.CurrentPage;

				//Showloader is false if control already shows loader
				showLoader = showLoader && (!loaderControl?.ShowLoader ?? false);

				if (showLoader)
				{
					var loaderTask = Application.StartTask(async () =>
					{
						await Task.Delay(LoaderDelay);

						lock (loaderControl)
						{
							if (!taskCompleted)
							{
								loaderControl!.ShowLoader = true;
								UpdateClient(context, true);
							}
						}
					});
				}
				

				var task = taskStarter();
				await task;

				if (showLoader)
				{
					lock (loaderControl)
					{
						taskCompleted = true;
						loaderControl!.ShowLoader = false;
					}
				}

				UpdateClient(context, true);

				sessionInfo.DecrementUpdates();
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

		public static bool IsUpdateInProgress(IWisejComponent context)
		{
			return GetSessionInfo(context).IsUpdateInProgress;
		}

		private class SessionUpdateInfo
		{
			public Form? CurrentForm = null;
			public Control? CurrentPage = null;
			private volatile int UpdatesInProgress = 0;
			public bool IsUpdateInProgress => this.UpdatesInProgress > 0;

			public void IncrementUpdates()
			{
				Interlocked.Increment(ref UpdatesInProgress);
			}

			public void DecrementUpdates()
			{
				Interlocked.Decrement(ref UpdatesInProgress);
			}
		}

		private static SessionUpdateInfo GetSessionInfo(IWisejComponent context)
		{
			SessionUpdateInfo? sessionInfo = null;
			if (Application.SessionId == null)
			{
				Application.RunInContext(context, () =>
				{
					lock (Application.Session)
					{
						sessionInfo = Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
					}
					
				});
			}
			else
			{
				lock (Application.Session)
				{
					sessionInfo = Application.Session.SessionUpdateInfo ??= new SessionUpdateInfo();
				}
			}

			return sessionInfo!;
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

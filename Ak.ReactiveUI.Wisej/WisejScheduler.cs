using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Wisej.Core;
using Wisej.Web;

namespace ReactiveUI.Wisej;

public class WisejScheduler : IScheduler
{
	private readonly IWisejComponent context;

	public WisejScheduler(IWisejComponent context)
	{
		this.context = context;
	}

	public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
	{
		var isCancelled = false; 
		var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };
		SessionUpdateHandler.UpdateClient(context, () =>
		{
			if (isCancelled)
			{
				return;
			}

			innerDisp.Disposable = action(this, state);
		}, showLoader: true);

		 return new CompositeDisposable(
			Disposable.Create(() => isCancelled = true),
			innerDisp);
	}

	public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
	{
		var token = new CancellationTokenSource();
		var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };

		if(dueTime < TimeSpan.Zero)
			dueTime = TimeSpan.Zero;

		var _ = Task.Delay(dueTime,token.Token).ContinueWith(task =>
		{
			if (task.IsCanceled)
				return;

			SessionUpdateHandler.UpdateClient(context, () =>
			{
				if (token.IsCancellationRequested)
					return;
			
				innerDisp.Disposable = action(this, state);
			});
		}, token.Token);

		return new CompositeDisposable(
			Disposable.Create(() => token.Cancel()),
			innerDisp);
	}

	public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
	{
		return this.Schedule(state, dueTime - DateTimeOffset.Now, action);
	}

	public DateTimeOffset Now => DateTimeOffset.Now;
}
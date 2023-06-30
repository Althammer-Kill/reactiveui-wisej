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
	private const int UpdateThrottleDelay = 100;
	
	private readonly IWisejComponent context;

	private SemaphoreSlim updateSemaphore = new SemaphoreSlim(0,1);

	private CancellationTokenSource cancelSource;

	public WisejScheduler(IWisejComponent context)
	{
		this.context = context;
		var task = UpdateTask(cancelSource.Token);
	}

	public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
	{
		var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };

		innerDisp.Disposable = action(this, state);

		if (SessionUpdateHandler.IsUpdateInProgress(context))
			return innerDisp;

		updateSemaphore.Release(1);

		return innerDisp;
	}

	private async Task UpdateTask(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			await updateSemaphore.WaitAsync(token);
			if (token.IsCancellationRequested)
				return;

			await Task.Delay(UpdateThrottleDelay, token);
			
			if (token.IsCancellationRequested)
				return;

			if (updateSemaphore.CurrentCount == 0)
			{
				SessionUpdateHandler.UpdateClient(context);
			}
		}
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

			if (token.IsCancellationRequested)
					return;
			
			innerDisp.Disposable = action(this, state);

			if (SessionUpdateHandler.IsUpdateInProgress(context))
				return;

			SessionUpdateHandler.UpdateClient(context);

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
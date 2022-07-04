// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Wisej.Web;

namespace ReactiveUI.Wisej
{
	/// <summary>
	/// This is an UserControl that is both a Form and has a ReactiveObject powers
	/// (i.e. you can call RaiseAndSetIfChanged).
	/// </summary>
	/// <typeparam name="T">The type of the view model.</typeparam>
	/// <seealso cref="Wisej.Web.Form" />
	/// <seealso cref="IViewFor{TViewModel}" />
	public partial class ReactiveForm<T> : Form, IViewFor<T>, INotifyPropertyChanged, ICanActivate
		where T : class, INotifyPropertyChanged
	{
		private readonly Subject<Unit> initSubject = new();
		private readonly Subject<Unit> deactivateSubject = new();
		private readonly CompositeDisposable compositeDisposable = new();

		private T? viewModel;

		private bool disposedValue; // To detect redundant calls

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <inheritdoc />
		public T? ViewModel
		{
			get => viewModel;
			set
			{
				if (EqualityComparer<T?>.Default.Equals(viewModel, value))
				{
					return;
				}

				viewModel = value;
				OnPropertyChanged();
			}
		}

		/// <inheritdoc />
		object? IViewFor.ViewModel
		{
			get => ViewModel;
			set => ViewModel = (T?)value;
		}

		public bool IsDesignerHosted
		{
			get
			{
				if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
					return true;

				Control ctrl = this;
				while (ctrl != null)
				{
					if ((ctrl.Site != null) && ctrl.Site.DesignMode)
						return true;
					ctrl = ctrl.Parent;
				}
				return false;
			}
		}

		/// <inheritdoc />
		public IObservable<Unit> Activated => initSubject.AsObservable();

		/// <inheritdoc />
		public IObservable<Unit> Deactivated => deactivateSubject.AsObservable();

		/// <inheritdoc/>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			initSubject.OnNext(Unit.Default);
		}

		/// <summary>
		/// Invokes the property changed event.
		/// </summary>
		/// <param name="propertyName">The name of the property.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		/// <summary>
		/// Cleans up the managed resources of the object.
		/// </summary>
		/// <param name="disposing">If it is getting called by the Dispose() method rather than a finalizer.</param>
		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					components?.Dispose();

					initSubject.Dispose();
					compositeDisposable.Dispose();
					deactivateSubject.OnNext(Unit.Default);
				}

				disposedValue = true;
			}
			base.Dispose(disposing);
		}

		protected virtual void BindViewModel(CompositeDisposable dr)
		{

		}
	}
}

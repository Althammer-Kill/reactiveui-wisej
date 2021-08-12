// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;

using Splat;
using System.Threading;

namespace ReactiveUI.Wisej
{
	public class Registrations
	{
		public void Register(Action<Func<object>, Type> registerFunction)
		{
			if (registerFunction is null)
			{
				throw new ArgumentNullException(nameof(registerFunction));
			}

			registerFunction(() => new PlatformOperations(), typeof(IPlatformOperations));

			registerFunction(() => new CreatesWisejCommandBinding(), typeof(ICreatesCommandBinding));
			registerFunction(() => new WisejCreatesObservableForProperty(), typeof(ICreatesObservableForProperty));
			registerFunction(() => new ActivationForViewFetcher(), typeof(IActivationForViewFetcher));
			registerFunction(() => new PanelSetMethodBindingConverter(), typeof(ISetMethodBindingConverter));
			registerFunction(() => new TableContentSetMethodBindingConverter(), typeof(ISetMethodBindingConverter));
			registerFunction(() => new ComponentModelTypeConverter(), typeof(IBindingTypeConverter));

			if (!ModeDetector.InUnitTestRunner())
			{
				RxApp.MainThreadScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current); // TODO
			}
		}
	}
}

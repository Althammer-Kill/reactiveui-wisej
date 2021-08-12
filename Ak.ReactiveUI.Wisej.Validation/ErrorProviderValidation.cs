using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Formatters;
using ReactiveUI.Validation.Formatters.Abstractions;
using ReactiveUI.Validation.ValidationBindings;
using Splat;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Wisej.Web;

namespace ReactiveUI.Wisej.Validation
{
	public static class ErrorProviderValidation
	{
		public static void BindValidation<TView, TViewModel, TViewModelProperty>(
			this TView view,
			TViewModel viewModel,
			Expression<Func<TViewModel, TViewModelProperty>> viewModelProperty,
			ErrorProvider errorProvider,
			Control control,
			IValidationTextFormatter<string>? formatter = null)
			where TView : IViewFor<TViewModel>
			where TViewModel : class, IReactiveObject, IValidatableViewModel
		{
			if (errorProvider is null)
			{
				throw new ArgumentNullException(nameof(errorProvider));
			}

			if (control is null)
			{
				throw new ArgumentNullException(nameof(control));
			}

			formatter = formatter
				?? Locator.Current.GetService<IValidationTextFormatter<string>>()
				?? SingleLineFormatter.Default;

			ValidationBinding.ForProperty(
				view,
				viewModelProperty,
				action: (states, errors) =>
				{
					StringBuilder str = new StringBuilder(errors.Sum(e => (e?.Length ?? 0) + 2));
					bool allValid = true;

					for (int i = 0; i < errors.Count; ++i)
					{
						if (!states[i].IsValid)
							allValid = false;

						if (str.Length > 0)
							str.AppendLine();

						str.Append(errors[i]);
					}

					if (allValid)
					{
						errorProvider.SetError(control, "");
					}
					else
					{
						errorProvider.SetError(control, str.ToString());
					}
				},
				formatter);
		}
	}
}

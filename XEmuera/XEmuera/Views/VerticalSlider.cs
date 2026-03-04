using System;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace XEmuera.Views
{
	/// <summary>
	/// A true vertical slider that responds to drag gestures along the Y axis.
	/// Thumb at the bottom corresponds to Value == Maximum (latest log content);
	/// thumb at the top corresponds to Value == Minimum.
	/// </summary>
	public class VerticalSlider : ContentView
	{
		private const double TrackWidth = 4;
		private const double ThumbWidth = 20;
		private const double ThumbHeight = 40;

		private readonly AbsoluteLayout layout;
		private readonly BoxView trackView;
		private readonly BoxView thumbView;

		private double startValue;

		// ── Bindable properties ──────────────────────────────────────────────

		public static readonly BindableProperty MinimumProperty = BindableProperty.Create(
			nameof(Minimum), typeof(double), typeof(VerticalSlider), 0.0,
			propertyChanged: (b, _, __) => ((VerticalSlider)b).UpdateThumbPosition());

		public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
			nameof(Maximum), typeof(double), typeof(VerticalSlider), 1.0,
			propertyChanged: (b, _, __) =>
			{
				var slider = (VerticalSlider)b;
				// Ensure Value stays within bounds when Maximum changes
				if (slider.Value > slider.Maximum)
					slider.Value = slider.Maximum;
				slider.UpdateThumbPosition();
			});

		public static readonly BindableProperty ValueProperty = BindableProperty.Create(
			nameof(Value), typeof(double), typeof(VerticalSlider), 1.0,
			propertyChanged: (b, oldVal, newVal) =>
			{
				var slider = (VerticalSlider)b;
				slider.UpdateThumbPosition();
				slider.ValueChanged?.Invoke(slider, new ValueChangedEventArgs((double)oldVal, (double)newVal));
			});

		public double Minimum
		{
			get => (double)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		public double Maximum
		{
			get => (double)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, ClampValue(value));
		}

		// ── Events ───────────────────────────────────────────────────────────

		public event EventHandler<ValueChangedEventArgs> ValueChanged;
		public event EventHandler DragStarted;
		public event EventHandler DragCompleted;

		// ── Constructor ──────────────────────────────────────────────────────

		public VerticalSlider()
		{
			WidthRequest = ThumbWidth;

			trackView = new BoxView
			{
				Color = Color.LightGray,
				WidthRequest = TrackWidth,
				CornerRadius = 2,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Fill,
			};

			thumbView = new BoxView
			{
				Color = Color.LightGray,
				WidthRequest = ThumbWidth,
				HeightRequest = ThumbHeight,
				CornerRadius = 4,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Start,
			};

			layout = new AbsoluteLayout();

			// Track spans full height, centered horizontally
			AbsoluteLayout.SetLayoutBounds(trackView, new Rectangle(0.5, 0, TrackWidth, 1));
			AbsoluteLayout.SetLayoutFlags(trackView,
				AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.HeightProportional);

			// Thumb – Y position is updated dynamically
			AbsoluteLayout.SetLayoutBounds(thumbView, new Rectangle(0.5, 0, ThumbWidth, ThumbHeight));
			AbsoluteLayout.SetLayoutFlags(thumbView, AbsoluteLayoutFlags.XProportional);

			layout.Children.Add(trackView);
			layout.Children.Add(thumbView);

			Content = layout;

			var pan = new PanGestureRecognizer();
			pan.PanUpdated += OnPanUpdated;
			GestureRecognizers.Add(pan);

			SizeChanged += OnSizeChanged;
		}

		// ── Private helpers ──────────────────────────────────────────────────

		private double ClampValue(double v) => Math.Max(Minimum, Math.Min(Maximum, v));

		/// <summary>
		/// Usable track length accounting for the thumb's height.
		/// </summary>
		private double TrackLength => Math.Max(0, Height - ThumbHeight);

		/// <summary>
		/// Returns the thumb's Y offset (from top) for the current Value.
		/// Thumb at bottom (Y = TrackLength) ↔ Value == Maximum.
		/// </summary>
		private double GetThumbY()
		{
			double range = Maximum - Minimum;
			if (range <= 0) return TrackLength;
			double normalized = (Value - Minimum) / range;
			return normalized * TrackLength;
		}

		private void UpdateThumbPosition()
		{
			if (Height <= 0) return;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				double y = GetThumbY();
				AbsoluteLayout.SetLayoutBounds(thumbView, new Rectangle(0.5, y, ThumbWidth, ThumbHeight));
				AbsoluteLayout.SetLayoutFlags(thumbView, AbsoluteLayoutFlags.XProportional);
			});
		}

		private void OnSizeChanged(object sender, EventArgs e)
		{
			UpdateThumbPosition();
		}

		private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
		{
			switch (e.StatusType)
			{
				case GestureStatus.Started:
					startValue = Value;
					DragStarted?.Invoke(this, EventArgs.Empty);
					break;

				case GestureStatus.Running:
					double trackLen = TrackLength;
					if (trackLen <= 0) break;
					double range = Maximum - Minimum;
					double delta = (e.TotalY / trackLen) * range;
					Value = ClampValue(startValue + delta);
					// ValueChanged is fired by the ValueProperty propertyChanged callback.
					break;

				case GestureStatus.Completed:
					DragCompleted?.Invoke(this, EventArgs.Empty);
					break;
			}
		}
	}
}

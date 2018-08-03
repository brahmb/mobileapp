using System;
using System.Globalization;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using MvvmCross.Base;
using MvvmCross.Converters;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.Views
{
    public class DurationEditText : EditText
    {
        private TimeSpan originalDuration;

        private TimeSpan duration;

        private DurationFieldInfo input = DurationFieldInfo.Empty;

        private bool isEditing;

        private readonly MvxValueConverter<TimeSpan, string> converter = new TimeSpanToDurationValueConverter();

        public event EventHandler DurationChanged;

        public TimeSpan Duration
        {
            get => duration;
            set
            {
                if (duration == value)
                    return;
                
                duration = value;

                if (isEditing == false)
                    showCurrentDuration();
            }
        }

        public DurationEditText(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            setupInputMode();
        }

        public DurationEditText(Context context) : base(context)
        {
            setupInputMode();
        }

        public DurationEditText(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            setupInputMode();
        }

        public DurationEditText(Context context, IAttributeSet attrs, int defStyleRes) : base(context, attrs, defStyleRes)
        {
            setupInputMode();
        }

        public DurationEditText(Context context, IAttributeSet attrs, int defStyleAttrs, int defStyleRes) : base(context, attrs, defStyleAttrs, defStyleRes)
        {
            setupInputMode();
        }

        private void setupInputMode()
        {
            TransformationMethod = null;

            var filter = new InputFilter();
            filter.onDigit += onDigitEntered;
            filter.onDelete += onDeleteEntered;

            SetFilters(new IInputFilter[] { filter });
        }

        private void showCurrentDuration()
        {
            var text = (string)converter?.Convert(duration, typeof(TimeSpan), null, CultureInfo.CurrentCulture) ?? string.Empty;
            Text = text;
        }

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, Android.Graphics.Rect previouslyFocusedRect)
        {
            if (gainFocus)
            {
                isEditing = true;
                originalDuration = duration;
                input = DurationFieldInfo.Empty;
                Text = input.ToString();
                moveCursorToEnd();
            }
            else
            {
                isEditing = false;
                showCurrentDuration();
                DurationChanged.Raise(this);
            }

            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
        }

        private void tryUpdate(DurationFieldInfo nextInput)
        {
            if (nextInput.Equals(input))
                return;

            input = nextInput;

            Duration = input.IsEmpty ? originalDuration : input.ToTimeSpan();

            Text = input.ToString();
        }

        public override void OnEditorAction(ImeAction actionCode)
        {
            if (actionCode == ImeAction.Done || actionCode == ImeAction.Next)
            {
                this.RemoveFocus();
                DurationChanged.Raise(this);
            }
        }

        protected override void OnTextChanged(ICharSequence text, int start, int lengthBefore, int lengthAfter)
        {
            if (isEditing)
            {
                if (Text != input.ToString())
                    Text = input.ToString();
            }
            else
            {
                if (Text != text.ToString())
                    Text = text.ToString();
            }

            moveCursorToEnd();
        }

        private void onDeleteEntered()
        {
            var nextInput = input.Pop();
            tryUpdate(nextInput);
        }

        private void onDigitEntered(int digit)
        {
            var nextInput = input.Push(digit);
            tryUpdate(nextInput);
        }

        protected override void OnSelectionChanged(int selStart, int selEnd)
        {
            moveCursorToEnd();
        }

        private void moveCursorToEnd()
        {
            SetSelection(Text.Length);
        }

        private class InputFilter : Java.Lang.Object, IInputFilter
        {
            public event Action<int> onDigit;
            public event Action onDelete;

            public ICharSequence FilterFormatted(ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
            {
                var empty = string.Empty.AsJavaString();
                var sourceLength = source.Length();

                if (sourceLength > 1)
                    return source.ToString().AsJavaString();

                if (sourceLength == 0)
                {
                    onDelete?.Invoke();
                    return empty;
                }

                var lastChar = source.CharAt(sourceLength - 1);

                if (char.IsDigit(lastChar))
                {
                    int digit = int.Parse(lastChar.ToString());
                    onDigit?.Invoke(digit);

                    return digit.ToString().AsJavaString();
                }

                return empty;
            }
        }
    }
}

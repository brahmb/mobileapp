using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Widget;
using Java.Lang;
using Toggl.Giskard.Autocomplete;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.AutocompleteEditText")]
    public sealed class AutocompleteEditText : EditText, ITextWatcher
    {
        private bool isEditingText = false;

        private readonly ISubject<Unit> positionChanged = new Subject<Unit>();

        public IObservable<ICharSequence> TextObservable { get; }

        public AutocompleteEditText(Context context, IAttributeSet attrs) 
            : base(context, attrs)
        {
            AddTextChangedListener(this);
            
            TextObservable =
                this.TextFormatted().CombineLatest(positionChanged.AsObservable(), (text, _) => text);
        }

        public AutocompleteEditText(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override void OnSelectionChanged(int selStart, int selEnd)
        {
            base.OnSelectionChanged(selStart, selEnd);

            positionChanged.OnNext(Unit.Default);
        }

        void ITextWatcher.BeforeTextChanged(ICharSequence sequence, int start, int count, int after)
        {
            if (isEditingText) 
                return;

            var isDeleting = count > after;
            if (!isDeleting) 
                return;

            var spannable = sequence as SpannableStringBuilder;
            var deletedSpan = spannable.GetSpans(start, start, Class.FromType(typeof(TokenSpan))).LastOrDefault();
            if (deletedSpan == null) 
                return;

            var spanStart = spannable.GetSpanStart(deletedSpan);
            var spanEnd = spannable.GetSpanEnd(deletedSpan);
            var isDeletingSpan = start < spanEnd;
            if (!isDeletingSpan)
                return;

            if (sequence is SpannableStringBuilder builder)
            {
                if (deletedSpan is TagsTokenSpan || deletedSpan is ProjectTokenSpan)
                {
                    isEditingText = true;
                    var newBuilder = new SpannableStringBuilder(sequence);
                    TextFormatted = newBuilder.Delete(spanStart, spanEnd);
                    SetSelection(spanStart);
                    isEditingText = false;
                }
            }
        }

        void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
        {
        }

        void ITextWatcher.AfterTextChanged(IEditable s)
        {
        }
    }
}

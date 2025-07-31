using System.Collections;
using System.Windows;
using WpfListBox = System.Windows.Controls.ListBox;
using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace GestLog.Behaviors
{
    public class MultiSelectListBoxBehavior : Behavior<WpfListBox>
    {
        public static readonly DependencyProperty SynchronizedSelectedItemsProperty =
            DependencyProperty.Register(
                nameof(SynchronizedSelectedItems),
                typeof(IList),
                typeof(MultiSelectListBoxBehavior),
                new PropertyMetadata(null, OnSynchronizedSelectedItemsChanged));

        public IList SynchronizedSelectedItems
        {
            get => (IList)GetValue(SynchronizedSelectedItemsProperty);
            set => SetValue(SynchronizedSelectedItemsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
                AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
                AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
            base.OnDetaching();
        }

        private void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SynchronizedSelectedItems == null) return;
            foreach (var item in e.RemovedItems)
                SynchronizedSelectedItems.Remove(item);
            foreach (var item in e.AddedItems)
                if (!SynchronizedSelectedItems.Contains(item))
                    SynchronizedSelectedItems.Add(item);
        }

        private static void OnSynchronizedSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // No-op: handled by SelectionChanged
        }
    }
}

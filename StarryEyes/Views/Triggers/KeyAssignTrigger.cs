﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using StarryEyes.Models;
using StarryEyes.Settings;
using StarryEyes.Settings.KeyAssigns;

namespace StarryEyes.Views.Triggers
{
    public sealed class KeyAssignTrigger : TriggerBase<UIElement>
    {
        public KeyAssignGroup Group
        {
            get => (KeyAssignGroup)GetValue(GroupProperty);
            set => SetValue(GroupProperty, value);
        }

        // Using a DependencyProperty as the backing store for Group.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupProperty =
            DependencyProperty.Register("Group", typeof(KeyAssignGroup), typeof(KeyAssignTrigger),
                new PropertyMetadata(KeyAssignGroup.Global));

        protected override void OnAttached()
        {
            AssociatedObject.PreviewKeyDown += HandleKeyPreview;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewKeyDown -= HandleKeyPreview;
            base.OnDetaching();
        }

        void HandleKeyPreview(object sender, KeyEventArgs e)
        {
            if (MainWindowModel.SuppressKeyAssigns) return;
            e.Handled = CheckActionKey(e.Key);
        }

        private bool CheckActionKey(Key key)
        {
            var window = Window.GetWindow(AssociatedObject);
            if (window == null) return false;
            var modifiers = Keyboard.Modifiers;
            TextBox tbox;
            if ((tbox = FocusManager.GetFocusedElement(window) as TextBox) != null)
            {
                if (Group == KeyAssignGroup.Input)
                {
                    // input area mode
                    // allow any keys but cursor keys only accept when text box is empty.
                    switch (key)
                    {
                        case Key.Up:
                        case Key.Down:
                        case Key.Left:
                        case Key.Back:
                        case Key.Right:
                            if (!String.IsNullOrEmpty(tbox.Text))
                            {
                                return false;
                            }
                            break;
                    }
                }
                else
                {
                    // normal area mode
                    // only allows modifiered keys
                    if (modifiers == ModifierKeys.None && key != Key.Escape &&
                        !(key < Key.F1 && key > Key.F24))
                    {
                        return false;
                    }
                }

                if (modifiers == ModifierKeys.Control)
                {
                    switch (key)
                    {
                        case Key.A:
                        case Key.C:
                        case Key.X:
                        case Key.V:
                        case Key.Y:
                        case Key.Z:
                            // above keys could not override
                            return false;
                    }
                }
            }
            return KeyAssignManager.CurrentProfile.GetAssigns(key, Group)
                                   .Where(b => b.Modifiers == modifiers)
                                   .SelectMany(b => b.Actions)
                                   .Where(KeyAssignManager.InvokeAction)
                                   .ToArray() // run all actions
                                   .Any();
        }
    }
}
#region Copyright Notice

//    Copyright 2011-2013 Eleftherios Aslanoglou
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

#region Using Directives

using System;
using System.Windows;

#endregion

namespace NBA_Stats_Tracker.Windows.MiscTools
{
    /// <summary>
    ///     Implements a window to display a message to the user that can be copied to the clipboard.
    ///     Window size adjusts depending on the contents.
    /// </summary>
    public partial class CopyableMessageWindow
    {
        private bool _beep;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CopyableMessageWindow" /> class.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="title">The title of the window.</param>
        /// <param name="align">The text alignment to be used for the message.</param>
        /// <param name="beep">If true, the system sound Beep will be played after the window is loaded.</param>
        public CopyableMessageWindow(String msg, String title = "NBA Stats Tracker", TextAlignment align = TextAlignment.Left, bool beep = false)
        {
            InitializeComponent();

            txbMsg.Text = msg;
            txbMsg.TextAlignment = align;
            Title = title;

            _beep = beep;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Handles the Click event of the btnCopyToClip control.
        ///     Copies the message displayed to the clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void btnCopyToClip_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txbMsg.Text);
            Title += " (copied to clipboard)";
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_beep)
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }
}
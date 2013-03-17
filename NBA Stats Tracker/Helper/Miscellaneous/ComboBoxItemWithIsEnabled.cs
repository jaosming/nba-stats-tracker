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

namespace NBA_Stats_Tracker.Helper.Miscellaneous
{
    /// <summary>
    ///     Implements a generic combo-box item with an IsEnabled property. Used to create items in combo-boxes that can't be selected
    ///     (e.g. group headers).
    /// </summary>
    public class ComboBoxItemWithIsEnabled
    {
        public ComboBoxItemWithIsEnabled(string item, bool isEnabled = true)
        {
            Item = item;
            IsEnabled = isEnabled;
        }

        public string Item { get; set; }
        public bool IsEnabled { get; set; }
    }
}
﻿#region Copyright Notice

// Created by Lefteris Aslanoglou, (c) 2011-2012
// 
// Implementation of thesis
// "Application Development for Basketball Statistical Analysis in Natural Language"
// under the supervision of Prof. Athanasios Tsakalidis & MSc Alexandros Georgiou,
// Computer Engineering & Informatics Department, University of Patras, Greece.
// 
// All rights reserved. Unless specifically stated otherwise, the code in this file should 
// not be reproduced, edited and/or republished without explicit permission from the 
// author.

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NBA_Stats_Tracker.Helper
{
    public static class ListExtensions
    {
        /// <summary>
        /// Sorts using the default IComparer of T
        /// </summary>
        public static void Sort<T>(this BindingList<T> bl)
        {
            sort(bl, null, null);
        }

        public static void Sort<T>(this BindingList<T> bl, IComparer<T> p_Comparer)
        {
            sort(bl, p_Comparer, null);
        }

        public static void Sort<T>(this BindingList<T> bl, Comparison<T> p_Comparison)
        {
            sort(bl, null, p_Comparison);
        }

        private static void sort<T>(this BindingList<T> bl, IComparer<T> p_Comparer, Comparison<T> p_Comparison)
        {
            //Extract items and sort separately
            var sortList = new List<T>();
            foreach (T item in bl)
            {
                sortList.Add(item);
            }
            if (p_Comparison == null)
            {
                sortList.Sort(p_Comparer);
            } //if
            else
            {
                sortList.Sort(p_Comparison);
            } //else

            //Disable notifications, rebuild, and re-enable notifications
            bool oldRaise = bl.RaiseListChangedEvents;
            bl.RaiseListChangedEvents = false;
            try
            {
                bl.Clear();
                sortList.ForEach(item => bl.Add(item));
            }
            finally
            {
                bl.RaiseListChangedEvents = oldRaise;
                bl.ResetBindings();
            }
        }

        public static void Sort<T>(this ObservableCollection<T> oc)
        {
            sort(oc, null, null);
        }

        public static void Sort<T>(this ObservableCollection<T> oc, IComparer<T> p_Comparer)
        {
            sort(oc, p_Comparer, null);
        }

        public static void Sort<T>(this ObservableCollection<T> oc, Comparison<T> p_Comparison)
        {
            sort(oc, null, p_Comparison);
        }

        private static void sort<T>(this ObservableCollection<T> oc, IComparer<T> p_Comparer, Comparison<T> p_Comparison)
        {
            //Extract items and sort separately
            var sortList = new List<T>();
            foreach (T item in oc)
            {
                sortList.Add(item);
            }
            if (p_Comparison == null)
            {
                sortList.Sort(p_Comparer);
            } //if
            else
            {
                sortList.Sort(p_Comparison);
            } //else

            oc.Clear();
            sortList.ForEach(item => oc.Add(item));
        }

        public static int KVPStringComparison(KeyValuePair<int, string> kvp1, KeyValuePair<int, string> kvp2)
        {
            return String.Compare(kvp1.Value, kvp2.Value);
        }
    }
}
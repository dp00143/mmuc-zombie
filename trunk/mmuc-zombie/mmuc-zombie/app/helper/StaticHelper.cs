﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls.Maps;
using System.Collections.Generic;
using System.Linq;

namespace mmuc_zombie.app.helper
{
    public class StaticHelper
    {


        public static T GetParentOfType<T>(DependencyObject item) where T : DependencyObject
        {
            if (item == null) throw new ArgumentNullException("item");
            T result = null;
            var parent = VisualTreeHelper.GetParent(item);
            if (parent == null) return result;
            else if (parent.GetType().IsSubclassOf(typeof(T)))
            {
                result = (T)parent;
            }
            else result = GetParentOfType<T>(parent);
            return result;
        }


        public static MyPolygon drawPolygon(List<MyLocation> list, Color clr)
        {
            MyPolygon newPolygon = new MyPolygon();
            // Defines the polygon fill details
            newPolygon.Locations = new LocationCollection();
            newPolygon.Fill = new SolidColorBrush(clr);
            newPolygon.Stroke = new SolidColorBrush(Colors.Red);
            newPolygon.StrokeThickness = 3;
            newPolygon.Opacity = 0.1;
            var newlist = list.OrderBy(x => x.number).ToList();
            foreach (MyLocation l in newlist)
            {
                newPolygon.Locations.Add(new System.Device.Location.GeoCoordinate(l.latitude, l.longitude));
            }
            return newPolygon;
        }
    }
}
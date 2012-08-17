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


    public class Location : MyParseObject
    {
      public double Latitude { get; set; }
      public double Longitude { get; set; }

      public Location() { }
      public Location(double latitude, double longitude)
      {
         Latitude = latitude;
         Longitude = longitude;
      }

    }


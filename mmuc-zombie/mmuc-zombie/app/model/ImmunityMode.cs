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


public class ImmunityMode : MyParseObject
    {
        public int duration { get; set; }
        public DateTime? deadline { get; set; }
        public string locationId { get; set; } //Saferoom
    }


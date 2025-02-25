﻿using Microsoft.Maps.MapControl.WPF;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Threading;
using WPF_HomeWork_15.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Media;
using JsonSerializer = System.Text.Json.JsonSerializer;

#nullable disable

namespace WPF_HomeWork_15.Views;

public partial class MainView : Window
{
    Dictionary<string, string> BakuBusLines = new Dictionary<string, string>()
    {
        {"M8",  "11156"}, {"M3", "11050"}, {"M2", "11049"}, {"M1", "11048"}, {"Q1", "11155"}, {"1", "11032"},
        {"2", "11035"}, {"3", "11037"}, {"5", "11031"}, {"6", "11033"}, {"7B", "11046"}, {"7A", "11045"}, {"10", "11150"},
        {"11", "11056"}, {"13", "11039"}, {"14", "11036"}, {"17", "11043"}, {"21", "11040"}, {"24", "11151"}, {"30", "11055"},
        {"32", "11152"}, {"35", "11153"}, {"88", "11047"}, {"88A", "11034"}, {"125", "11041"}, {"175", "11044"}, {"205", "11158"},
        {"211", "11157"}
    };

    Random rnd = new Random();
    DispatcherTimer timer = new DispatcherTimer();
    LocationCollection locs = new LocationCollection();

    private string SelectedBus = "-1";
    private int SelectedIndex = -1;
    private bool canRequest = true;

    private BakuBus _bakuBus;
    public BakuBus BakuBus
    {
        get { return _bakuBus; }
        set { _bakuBus = value; LoadBus(); }
    }

    public MainView()
    {
        InitializeComponent();
        BingMap.CredentialsProvider = new ApplicationIdCredentialsProvider(System.Configuration.ConfigurationManager.AppSettings["mapApi"]);
        DataContext = this;
        timer.Interval = new TimeSpan(0, 0, 0, 3);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        timer.Tick += new EventHandler(timerTick_event);
        GetBusListAsync();
        timer.Start();
    }

    async void LoadBus()
    {
        BingMap.Children.Clear();
        lBox.Items.Clear();
        SelectedIndex = -1;
        Pushpin pin123 = new Pushpin() { Location = new Location(40.4583, 49.7522), Content = "Resul" };
        BingMap.Children.Add(pin123);
        for (int i = 0; i < BakuBus.Buses.Count; i++)
        {
            if (SelectedBus != "-1")
            {
                if (SelectedBus == BakuBus.Buses[i].Attributes.DISPLAY_ROUTE_CODE)
                {
                    if (canRequest)
                    {
                        try
                        {
                            locs.Clear();
                            var jsonString = (JsonConvert.DeserializeObject(await new HttpClient().GetStringAsync("https://www.bakubus.az/az/ajax/getPaths/" + BakuBusLines[SelectedBus])) as JObject)["Forward"]["busstops"];
                            foreach (var item in jsonString)
                                locs.Add(new Location(double.Parse(item["latitude"].ToString(), NumberStyles.Float, CultureInfo.CreateSpecificCulture("fr-FR")), double.Parse(item["longitude"].ToString(), NumberStyles.Float, CultureInfo.CreateSpecificCulture("fr-FR"))));

                            canRequest = false;
                        }
                        catch (Exception)
                        {
                            canRequest = false;
                            MessageBox.Show("Bus Route Not Found");
                        }
                    }

                    if (locs is not null)
                    {
                        MapPolyline routeLine = new MapPolyline() { Locations = locs, Stroke = new SolidColorBrush(Colors.Blue), StrokeThickness = 5 };                        
                        BingMap.Children.Add(routeLine);
                    }

                    Pushpin pin = new Pushpin() { Tag = i, Name = "PushPin", Location = new Location(double.Parse(BakuBus.Buses[i].Attributes.LATITUDE, NumberStyles.Float, CultureInfo.CreateSpecificCulture("fr-FR")), double.Parse(BakuBus.Buses[i].Attributes.LONGITUDE, NumberStyles.Float, CultureInfo.CreateSpecificCulture("fr-FR"))), Template = (ControlTemplate)this.FindResource("customPushPin"), Content = BakuBus.Buses[i].Attributes.DISPLAY_ROUTE_CODE, Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256))) };
                    pin.MouseEnter += PushPin_MouseEnter;
                    pin.MouseLeave += PushPin_MouseLeave;
                    BingMap.Children.Add(pin);
                }
            }
            else
            {
                if (!lBox.Items.Contains(BakuBus.Buses[i].Attributes.DISPLAY_ROUTE_CODE))
                    lBox.Items.Add(BakuBus.Buses[i].Attributes.DISPLAY_ROUTE_CODE);
                lBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));
                Pushpin pin = new Pushpin() { Tag = i, Name = "PushPin", Location = new Location(double.Parse(BakuBus.Buses[i].Attributes.LATITUDE, NumberStyles.Float, CultureInfo.CreateSpecificCulture("fr-FR")), double.Parse(BakuBus.Buses[i].Attributes.LONGITUDE, NumberStyles.Float, CultureInfo.CreateSpecificCulture("fr-FR"))), Template = (ControlTemplate)this.FindResource("customPushPin"), Content = BakuBus.Buses[i].Attributes.DISPLAY_ROUTE_CODE, Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256))) };
                pin.MouseEnter += PushPin_MouseEnter;
                pin.MouseLeave += PushPin_MouseLeave;
                BingMap.Children.Add(pin);
            }
        }
    }

    private void PushPin_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Popup.IsOpen = false;
        var obj = (sender as Pushpin).Tag;
        int index = int.Parse(obj.ToString());
        Popup.IsOpen = true;
        PopuptBoxRoute.Text = BakuBus.Buses[index].Attributes.ROUTE_NAME;
        PopuptBoxPlate.Text = BakuBus.Buses[index].Attributes.PLATE;
        PopuptBoxCari.Text = $"Cari: {BakuBus.Buses[index].Attributes.CURRENT_STOP}";
        PopuptBoxNext.Text = $"Next: {BakuBus.Buses[index].Attributes.PREV_STOP}";
    }

    private void PushPin_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => Popup.IsOpen = false;

    async void GetBusListAsync()
    {
        var jsonString = "";
        if (bool.Parse(System.Configuration.ConfigurationManager.AppSettings["UseApi"]))
            jsonString = await new HttpClient().GetStringAsync("https://www.bakubus.az/az/ajax/apiNew1");
        else
            jsonString = await File.ReadAllTextAsync(Path.Combine(new DirectoryInfo($"../../../Services/").FullName, "bakubusApi.json"));
        BakuBus = JsonSerializer.Deserialize<BakuBus>(jsonString);
    }

    private void timerTick_event(object sender, EventArgs e) => GetBusListAsync();

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (SelectedIndex == -1)
        {
            SelectedBus = lBox.SelectedValue.ToString();
            SelectedIndex = lBox.SelectedIndex;
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        SelectedBus = "-1";
        canRequest = true;
    }
}

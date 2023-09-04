using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Drawing;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms;
using System.Diagnostics;
using System.IO;

namespace WindowsFormsApp1
{
    public class Location
    {
        GMap.NET.WindowsForms.GMapMarker marker;
        private string id;
        private PointLatLng point = new PointLatLng();

        public Location(string id, PointLatLng point)
        {
            this.id = id;
            this.point = point;
        }

        public string GetId()
        {
            return id;
        }
        public void GetFocus(GMapControl Map)
        {
            Map.Position = point;
        }

        public void AddMarker(GMapControl Map, GMapOverlay markers, GMarkerGoogleType markerType = GMarkerGoogleType.red)
        {
            GMapMarker marker = new GMarkerGoogle(
                    point,
                    markerType);
            this.marker = marker;
            markers.Markers.Add(marker);
            if (Map.Overlays.Count != 0)
            {
                Map.Overlays.Remove(markers);
            }
            Map.Overlays.Add(markers);

        }
        private void UpdatePoint(PointLatLng point) { 
            this.point = point;
        }

        public void UpdateMarker(GMapControl Map, GMapOverlay markers, PointLatLng newPosition) {
            int selected_marker = markers.Markers.IndexOf(marker);
            markers.Markers[selected_marker].Position = newPosition;
            UpdatePoint(newPosition);
            Map.Refresh();
        }

        public void UpdateMarkerGPS(GMapControl Map, GMapOverlay markers)
        {
            try
            {
                string cordsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                string cordsbaseFileName = "GPGGA.txt";
                string cordsbaseFilePath = Path.Combine(cordsFolderPath, cordsbaseFileName);
                string[] lines = File.ReadAllLines(cordsbaseFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("$GPGGA"))
                    {
                        string[] fields = line.Split(',');

                        string latitude = fields[2]; // Широта
                        string longitude = fields[4]; // Долгота

                        double latitudeValue = ParseNmeaCoordinate(latitude);
                        double longitudeValue = ParseNmeaCoordinate(longitude);
                        PointLatLng point = new PointLatLng(latitudeValue, longitudeValue);

                        UpdateMarker(Map, markers, point);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error updating marker: {ex.Message}");
            }
        }

        private static double ParseNmeaCoordinate(string nmeaCoordinate)
        {
            if (string.IsNullOrEmpty(nmeaCoordinate))
                return 0.0;

            // Разделение на градусы и минуты
            double degrees = double.Parse(nmeaCoordinate.Substring(0, 2));
            double minutes = double.Parse(nmeaCoordinate.Substring(2));

            // Преобразование в градусы
            double coordinate = degrees + (minutes / 60);

            return coordinate;
        }

        public void SwapMarker(GMapControl Map, GMapOverlay markers)
        {
            Array markerTypes = Enum.GetValues(typeof(GMarkerGoogleType));
            Random random = new Random();
            int randomIndex = random.Next(markerTypes.Length);
            GMarkerGoogleType randomMarkerType = (GMarkerGoogleType)markerTypes.GetValue(randomIndex);

            markers.Markers.Remove(marker);
            marker = new GMarkerGoogle(
                    point,
                    randomMarkerType);
            markers.Markers.Add(marker);
            try
            {
                Map.Refresh();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error refresh map: "+ ex.Message);
            }
        }
    }
}

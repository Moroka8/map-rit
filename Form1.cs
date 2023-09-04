using GMap.NET.MapProviders;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Device.Location;
using System.Windows;
using System.Windows.Input;
using static GMap.NET.Entity.OpenStreetMapGraphHopperGeocodeEntity;
using GMap.NET.WindowsForms;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using GMap.NET.WindowsForms.Markers;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        bool isDragging = false;
        PointLatLng startLatLng;
        PointLatLng origLatLng;
        List<Location> locations = new List<Location>();
        GMapMarker last_selected_marker;
        GMapMarker selected_marker;
        GMapOverlay markers = new GMapOverlay("markers");
        GMapOverlay polygons = new GMapOverlay("polygons");
        string connectionString;

        public Form1()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            InitializeComponent();

            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
            }
            InitializeDatabase();

            LoadDataFromDatabase();
            locations[0].UpdateMarkerGPS(Map,markers);
            CreatePolygon();

        }

        private void CreatePolygon()
        {
            List<PointLatLng> points = new List<PointLatLng>
            {
                new PointLatLng(55.042107, 82.917427),
                new PointLatLng(55.044182, 82.941239),
                new PointLatLng(55.038735, 82.942651),
                new PointLatLng(55.036576, 82.918908) 
            };
            GMapPolygon polygon = new GMapPolygon(points, "Polygon");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red));
            polygon.Stroke = new Pen(Color.Red, 2);
            polygon.IsHitTestVisible = true;

            polygons.Polygons.Add(polygon);
            Map.Overlays.Add(polygons);
        }

        private void InitializeDatabase()
        {
            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            string databaseFileName = "DATABASE1.MDF";
            string databaseFilePath = Path.Combine(dataFolderPath, databaseFileName);
            connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={databaseFilePath};Integrated Security=True";

            if (!DatabaseExists(databaseFilePath))
            {
                DeleteExistingDatabase(databaseFilePath);

                CreateDatabase(databaseFilePath);
                CreateTable();
                InsertInitialData();
            }
        }

        private bool DatabaseExists(string databaseFilePath)
        {
            return File.Exists(databaseFilePath);
        }

        private void DeleteExistingDatabase(string databaseFilePath)
        {
            try
            {
                SqlConnection.ClearAllPools();
                string connectionStringMaster = "Server=(localdb)\\mssqllocaldb;Database=master;Integrated Security=True;";
                using (SqlConnection connectionMaster = new SqlConnection(connectionStringMaster))
                {
                    connectionMaster.Open();
                    string sqlExpression = $"DROP DATABASE IF EXISTS DATABASE1";
                    SqlCommand command = new SqlCommand(sqlExpression, connectionMaster);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error deleting existing database: {ex.Message}");
                MessageBox.Show("Error deleting existing database: " + ex.Message);
            }
        }

        private void CreateDatabase(string databaseFilePath)
        {
            string connectionStringMaster = "Server=(localdb)\\mssqllocaldb;Database=master;Integrated Security=True;";
            SqlConnection connectionMaster = new SqlConnection(connectionStringMaster);
            connectionMaster.Open();

            string sqlExpressionCreateDb = $"CREATE DATABASE DATABASE1 ON (NAME = DATABASE1, FILENAME='{databaseFilePath}')";
            SqlCommand commandCreateDb = new SqlCommand(sqlExpressionCreateDb, connectionMaster);
            commandCreateDb.ExecuteNonQuery();

            connectionMaster.Close();
            SqlConnection.ClearAllPools();
        }

        private void CreateTable()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string sqlCreateTable =
                    "CREATE TABLE Markers " +
                    "([Id]  INT PRIMARY KEY  IDENTITY (1, 1) NOT NULL," +
                    "[Lat] FLOAT (53) NOT NULL," +
                    "[Lng] FLOAT (53) NOT NULL)";
                SqlCommand createTableCommand = new SqlCommand(sqlCreateTable, connection);
                createTableCommand.ExecuteNonQuery();

                connection.Close();
            }
        }

        private void InsertInitialData()
        {
            string sqlInsertData1 = "INSERT INTO Markers (Lat, Lng) VALUES (54.974015, 82.924238)";
            string sqlInsertData2 = "INSERT INTO Markers (Lat, Lng) VALUES (55.030302, 82.923161)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand insertDataCommand1 = new SqlCommand(sqlInsertData1, connection);
                insertDataCommand1.ExecuteNonQuery();

                SqlCommand insertDataCommand2 = new SqlCommand(sqlInsertData2, connection);
                insertDataCommand2.ExecuteNonQuery();
            }
        }

        private void LoadDataFromDatabase()
        {
            string sqlExpression = "SELECT * FROM Markers";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        object id = reader.GetValue(0);
                        object lat = reader.GetValue(1);
                        object lng = reader.GetValue(2);
                        PointLatLng point = new PointLatLng(Convert.ToSingle(lat), Convert.ToSingle(lng));
                        Location location = new Location($"{id}", point);
                        location.AddMarker(Map, markers);
                        locations.Add(location);
                    }
                }

                reader.Close();
            }
        }

        private void MarkerSave()
        {
            double lat = last_selected_marker.Position.Lat;
            double lng = last_selected_marker.Position.Lng;
            int ind = markers.Markers.IndexOf(last_selected_marker);
            string id = locations[ind].GetId();
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string sqlExpression = "SELECT COUNT(*) FROM Markers";
            SqlCommand command = new SqlCommand(sqlExpression, connection);
            int markers_count = Convert.ToInt32(command.ExecuteScalar());
            if (Convert.ToInt32(id) > markers_count)
            {
                sqlExpression = $"INSERT INTO Markers (Lat, Lng) VALUES ({lat},{lng})";
                command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();
            }
            else
            {
                sqlExpression =
                    $"UPDATE Markers " +
                    $"SET Lat={lat}, Lng={lng} " +
                    $"WHERE Id={id}";
                command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();
            }
            Trace.WriteLine(sqlExpression);
            connection.Close();
        }

        private void CreateRandomMarker() 
        {
            RectLatLng viewArea = Map.ViewArea;
            Random random = new Random();
            double lat = random.NextDouble() * (viewArea.Bottom - viewArea.Top) + viewArea.Top;
            double lng = random.NextDouble() * (viewArea.Right - viewArea.Left) + viewArea.Left;
            PointLatLng point = new PointLatLng(lat, lng);

            string sqlExpression = "SELECT COUNT(*) FROM Markers";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                int markers_count = Convert.ToInt32(command.ExecuteScalar());
                int id = markers_count + 1;

                Location location = new Location($"{id}", point);
                location.AddMarker(Map, markers);
                locations.Add(location);
            }
        }

        private void Map_Load(object sender, EventArgs e)
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            Map.MapProvider = GoogleMapProvider.Instance;

            Map.MinZoom = 2;
            Map.MaxZoom = 17;
            Map.Zoom = 10;

            Map.Position = new PointLatLng(54.974015, 82.924238);

            Map.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            Map.CanDragMap = true;
            Map.DragButton = MouseButtons.Left;

        }

        private void Map_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
            }
            if (selected_marker != null)
            {
                MarkerSave();
            }
        }

        private void Map_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                startLatLng = Map.FromLocalToLatLng(e.X, e.Y);
            }
        }

        private void Map_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isDragging)
            {
                try
                {
                    PointLatLng currentLatLng = Map.FromLocalToLatLng(e.X, e.Y);
                    double deltaLat = currentLatLng.Lat - startLatLng.Lat;
                    double deltaLng = currentLatLng.Lng - startLatLng.Lng;
                    PointLatLng point = new PointLatLng(origLatLng.Lat+deltaLat, origLatLng.Lng+deltaLng);
                    int ind = markers.Markers.IndexOf(selected_marker);
                    locations[ind].UpdateMarker(Map,markers,point);
                }
                catch { 
                    
                }
            }
        }

        private void Map_OnMarkerEnter(GMapMarker item)
        {
            selected_marker = item;
            last_selected_marker = selected_marker;
            origLatLng = selected_marker.Position;
        }

        private void Map_OnMarkerLeave(GMapMarker item)
        {
            selected_marker = null;
        }

        private void Map_OnPolygonEnter(GMapPolygon item)
        {
            if (selected_marker != null)
            {
                if (polygonAction.SelectedIndex == 0)
                {
                    MessageBox.Show("Marker inside polygon");
                }
                else if (polygonAction.SelectedIndex == 1)
                {
                    int ind = markers.Markers.IndexOf(selected_marker);
                    locations[ind].SwapMarker(Map, markers);
                    selected_marker = markers.Markers[ind];
                    last_selected_marker = selected_marker;
                }
                else if (polygonAction.SelectedIndex == 2)
                {
                    CreateRandomMarker();
                }
            }
        }
    }
}

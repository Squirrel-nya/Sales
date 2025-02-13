using System;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Sales
{
    public partial class MainWindow : Window
    {
        private string connectionString;
        
        public MainWindow()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["PostgreSQLConnection"].ConnectionString;
            LoadManagers();
            LoadSections();
            LoadStatistics();
            LoadDealsTable();
        }
        
        private void LoadManagers()
        {
            cmbManagers.Items.Clear();
            cmbManagers.Items.Add("Все менеджеры"); // Возможность снять выбор
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var command = new NpgsqlCommand("SELECT manager_id, full_name FROM managers", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    cmbManagers.Items.Add(new SalesManager { Id = reader.GetInt32(0), Name = reader.GetString(1) });
                }
            }
            cmbManagers.SelectedIndex = 0;
        }
        
        private void LoadSections()
        {
            cmbSections.Items.Clear();
            cmbSections.Items.Add("Все секции"); // Возможность снять выбор
            cmbSections.Items.Add("Теплые продажи");
            cmbSections.Items.Add("Холодные продажи");
            cmbSections.SelectedIndex = 0;
        }
        
        private void LoadStatistics()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();// Получение общего количества сделок
                string totalDealsQuery = "SELECT COUNT(deal_id) FROM deals " +
                                         "JOIN managers m ON deals.manager_id = m.manager_id " +
                                         "JOIN deal_statuses s ON deals.deal_status_id = s.deal_status_id " +
                                         "WHERE (@manager IS NULL OR deals.manager_id = @manager::INTEGER) " +
                                         "AND (@date IS NULL OR deal_date = @date::DATE) " +
                                         "AND (@section IS NULL OR m.section = @section::TEXT)";
                
                using (var totalCommand = new NpgsqlCommand(totalDealsQuery, connection))
                {
                    var selectedManager = (cmbManagers?.SelectedItem as SalesManager) ?? null;
                    var managerId = (selectedManager == null || cmbManagers == null || cmbManagers.SelectedIndex == 0) ? (object)DBNull.Value : selectedManager.Id;
                    var dateValue = datePicker == null || datePicker.SelectedDate == null ? (object)DBNull.Value : datePicker.SelectedDate.Value.Date;
                    var sectionValue = cmbSections == null || cmbSections.SelectedIndex == 0 ? (object)DBNull.Value : (object)(cmbSections?.SelectedItem?.ToString() ?? "");
                    
                    totalCommand.Parameters.Add(new NpgsqlParameter("@manager", NpgsqlDbType.Integer) { Value = managerId });
                    totalCommand.Parameters.Add(new NpgsqlParameter("@date", NpgsqlDbType.Date) { Value = dateValue });
                    totalCommand.Parameters.Add(new NpgsqlParameter("@section", NpgsqlDbType.Text) { Value = sectionValue });
                    
                    int totalDeals = Convert.ToInt32(totalCommand.ExecuteScalar());
                    lblDealStats.Content = "Общее количество сделок: " + totalDeals + " \n";
                    
                    
                }
                
                // Получение количества сделок по статусам
                string query = "SELECT ds.status_name, COUNT(d.deal_id) FROM deals d " +
                               "JOIN deal_statuses ds ON d.deal_status_id = ds.deal_status_id " +
                               "JOIN managers m ON d.manager_id = m.manager_id " +
                               "WHERE (@manager IS NULL OR d.manager_id = @manager::INTEGER) " +
                               "AND (@date IS NULL OR d.deal_date = @date::DATE) " +
                               "AND (@section IS NULL OR m.section = @section::TEXT) " +
                               "GROUP BY ds.status_name";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    var selectedManager = (cmbManagers?.SelectedItem as SalesManager) ?? null;
                    var managerId = (selectedManager == null || cmbManagers == null || cmbManagers.SelectedIndex == 0) ? (object)DBNull.Value : selectedManager.Id;
                    var dateValue = datePicker == null || datePicker.SelectedDate == null ? (object)DBNull.Value : datePicker.SelectedDate.Value.Date;
                    var sectionValue = cmbSections == null || cmbSections.SelectedIndex == 0 ? (object)DBNull.Value : (object)(cmbSections?.SelectedItem?.ToString() ?? "");

                    command.Parameters.Add(new NpgsqlParameter("@manager", NpgsqlDbType.Integer) { Value = managerId });
                    command.Parameters.Add(new NpgsqlParameter("@date", NpgsqlDbType.Date) { Value = dateValue });
                    command.Parameters.Add(new NpgsqlParameter("@section", NpgsqlDbType.Text) { Value = sectionValue });

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lblDealStats.Content += reader.GetString(0) + ": " + reader.GetInt32(1) + "\n";
                        }
                    }
                }
                
                // Получение количества звонков по статусам
               string callStatsQuery = "SELECT COUNT(call_id), " +
                                        "SUM(CASE WHEN call_status_id = 1 THEN 1 ELSE 0 END) AS successful_calls, " +
                                        "SUM(CASE WHEN call_status_id = 2 THEN 1 ELSE 0 END) AS missed_calls " +
                                        "FROM calls " +
                                        "JOIN managers m ON calls.manager_id = m.manager_id " +
                                        "WHERE (@manager IS NULL OR calls.manager_id = @manager::INTEGER) " +
                                        "AND (@date IS NULL OR call_date = @date::DATE)"+
                                        "AND (@section IS NULL OR m.section = @section::TEXT) ";

                using (var callStatsCommand = new NpgsqlCommand(callStatsQuery, connection))
                {
                    var selectedManager = (cmbManagers?.SelectedItem as SalesManager) ?? null;
                    var managerId = (selectedManager == null || cmbManagers == null || cmbManagers.SelectedIndex == 0) ? (object)DBNull.Value : selectedManager.Id;
                    var dateValue = datePicker == null || datePicker.SelectedDate == null ? (object)DBNull.Value : datePicker.SelectedDate.Value.Date;
                    var sectionValue = cmbSections == null || cmbSections.SelectedIndex == 0 ? (object)DBNull.Value : (object)(cmbSections?.SelectedItem?.ToString() ?? "");

                    callStatsCommand.Parameters.Add(new NpgsqlParameter("@manager", NpgsqlDbType.Integer) { Value = managerId });
                    callStatsCommand.Parameters.Add(new NpgsqlParameter("@date", NpgsqlDbType.Date) { Value = dateValue });
                    callStatsCommand.Parameters.Add(new NpgsqlParameter("@section", NpgsqlDbType.Text) { Value = sectionValue });

                        using (var reader = callStatsCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int totalCalls = reader.GetInt32(0);
                                int successfulCalls = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                                int missedCalls = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            
                                lblCallsStats.Content = "Общее количество звонков: " + totalCalls + " \n";
                                lblCallsStats.Content += "Успешные звонки: " + successfulCalls + " \n";
                                lblCallsStats.Content += "Неотвеченные звонки: " + missedCalls + " ";
                            }
                    }
                }
            }
        }
        
        private void LoadDealsTable()
        {
            if (dealsDataGrid == null) return;
            
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT d.deal_id AS \"ID\", TO_CHAR(d.deal_date, 'DD.MM.YYYY') AS \"Дата\", m.full_name AS \"Менеджер\", m.section AS \"Секция\", s.status_name AS \"Статус\"FROM deals d " +
                               "JOIN managers m ON d.manager_id = m.manager_id " +
                               "JOIN deal_statuses s ON d.deal_status_id = s.deal_status_id " +
                               "WHERE (@manager IS NULL OR d.manager_id = @manager::INTEGER) " +
                               "AND (@date IS NULL OR d.deal_date = @date::DATE) " +
                               "AND (@section IS NULL OR m.section = @section::TEXT)";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    var selectedManager = (cmbManagers?.SelectedItem as SalesManager) ?? null;
                    var managerId = (selectedManager == null || cmbManagers == null || cmbManagers.SelectedIndex == 0) ? (object)DBNull.Value : selectedManager.Id;
                    var dateValue = datePicker == null || datePicker.SelectedDate == null ? (object)DBNull.Value : datePicker.SelectedDate.Value.Date;
                    var sectionValue = cmbSections == null || cmbSections.SelectedIndex == 0 ? (object)DBNull.Value : (object)(cmbSections?.SelectedItem?.ToString() ?? "");

                    command.Parameters.Add(new NpgsqlParameter("@manager", NpgsqlDbType.Integer) { Value = managerId });
                    command.Parameters.Add(new NpgsqlParameter("@date", NpgsqlDbType.Date) { Value = dateValue });
                    command.Parameters.Add(new NpgsqlParameter("@section", NpgsqlDbType.Text) { Value = sectionValue });
                    using (var reader = command.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        dealsDataGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
        }
        
        private void LoadCallsTable()
        {
            if (callsDataGrid == null) return;
            
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT c.call_id AS \"ID\", TO_CHAR(c.call_date, 'DD.MM.YYYY') AS \"Дата\", TO_CHAR(c.call_time, 'HH24:MI:SS') AS \"Время\", m.full_name AS \"Менеджер\", cs.status_name AS \"Статус\"FROM calls c " +
                               "JOIN managers m ON c.manager_id = m.manager_id " +
                               "JOIN call_statuses cs ON c.call_status_id = cs.call_status_id " +
                               "WHERE (@manager IS NULL OR c.manager_id = @manager::INTEGER) " +
                               "AND (@date IS NULL OR c.call_date = @date::DATE)"+
                               "AND (@section IS NULL OR m.section = @section::TEXT)";
                
                using (var command = new NpgsqlCommand(query, connection))
                {
                    var selectedManager = (cmbManagers?.SelectedItem as SalesManager) ?? null;
                    var managerId = (selectedManager == null || cmbManagers == null || cmbManagers.SelectedIndex == 0) ? (object)DBNull.Value : selectedManager.Id;
                    var dateValue = datePicker == null || datePicker.SelectedDate == null ? (object)DBNull.Value : datePicker.SelectedDate.Value.Date;
                    var sectionValue = cmbSections == null || cmbSections.SelectedIndex == 0 ? (object)DBNull.Value : (object)(cmbSections?.SelectedItem?.ToString() ?? "");
                    
                    command.Parameters.Add(new NpgsqlParameter("@manager", NpgsqlDbType.Integer) { Value = managerId });
                    command.Parameters.Add(new NpgsqlParameter("@date", NpgsqlDbType.Date) { Value = dateValue });
                    command.Parameters.Add(new NpgsqlParameter("@section", NpgsqlDbType.Text) { Value = sectionValue });
                    
                    using (var reader = command.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        callsDataGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
        }
        
        private void FiltersChanged(object sender, EventArgs e)
        {
            LoadStatistics();
            LoadDealsTable();
            LoadCallsTable(); 
        }
    }
}

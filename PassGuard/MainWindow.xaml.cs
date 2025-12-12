using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace PassGuard
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<PasswordEntry> _allEntries = new ObservableCollection<PasswordEntry>();
        private string _masterPassword = "";
        private bool _isLoggedIn = false;
        private readonly string _dataFile = "passwords.dat";
        private readonly string _settingsFile = "settings.dat";

        public MainWindow()
        {
            InitializeComponent();
            LoadSampleData();
            UpdateUIState();
        }

        private void LoadSampleData()
        {
            if (!File.Exists(_dataFile))
            {
                _allEntries.Add(new PasswordEntry
                {
                    Id = 1,
                    Title = "Google",
                    Username = "user@gmail.com",
                    Category = "Email",
                    CreatedDate = DateTime.Now.AddDays(-30)
                });

                _allEntries.Add(new PasswordEntry
                {
                    Id = 2,
                    Title = "GitHub",
                    Username = "developer",
                    Category = "Work",
                    CreatedDate = DateTime.Now.AddDays(-15)
                });

                PasswordsGrid.ItemsSource = _allEntries;
            }
        }

        private void UpdateUIState()
        {
            bool canEdit = _isLoggedIn && PasswordsGrid.SelectedItem != null;
            EditButton.IsEnabled = canEdit;
            DeleteButton.IsEnabled = canEdit;
            ShowPasswordButton.IsEnabled = canEdit;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string password = MasterPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter master password");
                return;
            }

            try
            {
                if (CryptoService.ValidateMasterPassword(password, _settingsFile))
                {
                    _masterPassword = password;
                    _isLoggedIn = true;

                    // Загружаем реальные данные
                    var entries = DataService.LoadData(_dataFile, _masterPassword);
                    _allEntries.Clear();
                    foreach (var entry in entries)
                    {
                        _allEntries.Add(entry);
                    }

                    PasswordsGrid.ItemsSource = _allEntries;
                    UpdateUIState();

                    MessageBox.Show("Login successful!");
                }
                else
                {
                    MessageBox.Show("Invalid password");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoggedIn)
            {
                MessageBox.Show("Please login first");
                return;
            }

            var dialog = new AddEditWindow();
            if (dialog.ShowDialog() == true && dialog.PasswordEntry != null)
            {
                dialog.PasswordEntry.Id = _allEntries.Count > 0 ? _allEntries.Max(e => e.Id) + 1 : 1;
                dialog.PasswordEntry.CreatedDate = DateTime.Now;

                _allEntries.Add(dialog.PasswordEntry);
                DataService.SaveData(_allEntries.ToList(), _dataFile, _masterPassword);
                PasswordsGrid.Items.Refresh();

                MessageBox.Show("Added!");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoggedIn || PasswordsGrid.SelectedItem is not PasswordEntry selectedEntry)
            {
                MessageBox.Show("Select an item to edit");
                return;
            }

            var dialog = new AddEditWindow(selectedEntry);
            if (dialog.ShowDialog() == true && dialog.PasswordEntry != null)
            {
                var index = _allEntries.IndexOf(selectedEntry);
                if (index >= 0)
                {
                    _allEntries[index] = dialog.PasswordEntry;
                    DataService.SaveData(_allEntries.ToList(), _dataFile, _masterPassword);
                    PasswordsGrid.Items.Refresh();

                    MessageBox.Show("Updated!");
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoggedIn || PasswordsGrid.SelectedItem is not PasswordEntry selectedEntry)
                return;

            var result = MessageBox.Show("Delete '" + selectedEntry.Title + "'?",
                "Confirm", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                _allEntries.Remove(selectedEntry);
                DataService.SaveData(_allEntries.ToList(), _dataFile, _masterPassword);
                PasswordsGrid.Items.Refresh();

                MessageBox.Show("Deleted!");
            }
        }

        private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoggedIn || PasswordsGrid.SelectedItem is not PasswordEntry selectedEntry)
                return;

            try
            {
                var password = CryptoService.Decrypt(selectedEntry.EncryptedPassword, _masterPassword);
                MessageBox.Show($"Password for {selectedEntry.Title}:\n\n{password}",
                    "Password");
            }
            catch
            {
                MessageBox.Show("Cannot decrypt password");
            }
        }

        private void GeneratePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            bool useUppercase = UppercaseCheckBox?.IsChecked == true;
            bool useDigits = DigitsCheckBox?.IsChecked == true;

            var password = PasswordGenerator.Generate(12, useUppercase, useDigits);
            GeneratedPasswordBox.Text = password;
        }

        private void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GeneratedPasswordBox.Text))
            {
                Clipboard.SetText(GeneratedPasswordBox.Text);
                MessageBox.Show("Copied to clipboard");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoggedIn) return;

            var searchText = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                PasswordsGrid.ItemsSource = _allEntries;
            }
            else
            {
                var filtered = _allEntries.Where(entry =>
                    entry.Title.ToLower().Contains(searchText) ||
                    entry.Username.ToLower().Contains(searchText) ||
                    entry.Category?.ToLower().Contains(searchText) == true).ToList();

                PasswordsGrid.ItemsSource = filtered;
            }
        }

        private void PasswordsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUIState();
        }
    }

    public class PasswordEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Username { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
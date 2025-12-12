using System.Windows;

namespace PassGuard
{
    public partial class AddEditWindow : Window
    {
        public PasswordEntry PasswordEntry { get; private set; }

        public AddEditWindow(PasswordEntry existingEntry = null)
        {
            InitializeComponent();

            if (existingEntry != null)
            {
                PasswordEntry = existingEntry;
                TitleTextBox.Text = existingEntry.Title;
                UsernameTextBox.Text = existingEntry.Username;
                PasswordBox.Password = existingEntry.EncryptedPassword;
                Title = "Edit Entry";
            }
            else
            {
                PasswordEntry = new PasswordEntry();
                Title = "Add Entry";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Enter title");
                return;
            }

            PasswordEntry.Title = TitleTextBox.Text;
            PasswordEntry.Username = UsernameTextBox.Text;
            PasswordEntry.EncryptedPassword = PasswordBox.Password;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var password = PasswordGenerator.GenerateSimple();
            PasswordBox.Password = password;
        }
    }
}

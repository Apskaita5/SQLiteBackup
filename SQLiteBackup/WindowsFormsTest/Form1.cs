using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void openFileButton_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = false;
                if (ofd.ShowDialog() == DialogResult.OK) this.filePathTextBox.Text = ofd.FileName;
            }
        }

        private void openDatabaseButton_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = false;
                if (ofd.ShowDialog() == DialogResult.OK) this.databasePathtextBox.Text = ofd.FileName;
            }
        }

        private void exportButton_Click(object sender, EventArgs e)
        {

            var m_dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", databasePathtextBox.Text));
            m_dbConnection.Open();
            SQLiteCommand command = new SQLiteCommand(m_dbConnection);

            var parameters = new ExportInformations();
            parameters.ExportRows = true;
            parameters.ExportTableStructure = true;
            parameters.ExportTriggers = true;
            parameters.ExportViews = true;
            parameters.RowsExportMode = RowsDataExportMode.Insert;

            var error = false;

            try
            {
                var backup = new SQLiteBackup(command);
                backup.ExportInfo = parameters;
                backup.ExportToFile(filePathTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                error = true;
            }
            finally
            {
                m_dbConnection.Close();
                m_dbConnection.Dispose();
            }   
            
            if (!error) MessageBox.Show("Export completed.");

        }

        private void ImportButton_Click(object sender, EventArgs e)
        {

            var m_dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", databasePathtextBox.Text));
            m_dbConnection.Open();
            SQLiteCommand command = new SQLiteCommand(m_dbConnection);

            var parameters = new ImportInformations();
            parameters.IgnoreSqlError = false;

            var error = false;

            try
            {
                var backup = new SQLiteBackup(command);
                backup.ImportInfo = parameters;
                backup.ImportFromFile(filePathTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                error = true;
            }
            finally
            {
                m_dbConnection.Close();
                m_dbConnection.Dispose();
            }

            if (!error) MessageBox.Show("Import completed.");

        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBSP2ClassGen
{
    public partial class MainForm : Form
    {
        public string IniFileName; 

        public MainForm()
        {
            InitializeComponent();

            txtIP.Text = "127.0.0.1";
            txtPort.Text = "1433";
            txtUsername.Text = "sa";
            txtPassword.Text = "123456";
            cbxDatabaseList.Text = "gameuserdb";
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowPw.Checked)  txtPassword.UseSystemPasswordChar = false;                
            if (!chkShowPw.Checked) txtPassword.UseSystemPasswordChar = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.IniFileName = openFileDialog1.FileName;
                lblAdHoc.Text = "Ad-hoc Query File : " + this.IniFileName;                
            }
        }

        private void cbxDatabaseList_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }
        private void cbxDatabaseList_DropDown(object sender, EventArgs e)
        {
            if( txtIP.Text.ToString().Length != 0 && 
                txtPort.Text.ToString().Length != 0 && 
                txtUsername.Text.ToString().Length != 0 &&
                txtPassword.Text.ToString().Length != 0 )
            {
                List<string> dblist; 
                DBSP2ClassGen.dbhandler d = new DBSP2ClassGen.dbhandler();

                dblist = d.GetDatabaseList(txtIP.Text.ToString(), txtPort.Text.ToString(), txtUsername.Text.ToString(), txtPassword.Text.ToString());

                cbxDatabaseList.Items.Clear();

                foreach(string dbname in dblist )
                {
                    cbxDatabaseList.Items.Add(dbname);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(txtIP.Text.ToString().Length == 0 && 
               txtPort.Text.ToString().Length == 0 && 
               txtUsername.Text.ToString().Length == 0 &&
               txtPassword.Text.ToString().Length == 0 &&
               cbxDatabaseList.Text.ToString().Length == 0 ) 
            {
                MessageBox.Show("Fill the empty field(s).","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (SavefolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String SaveDir = SavefolderBrowserDialog.SelectedPath;

                DBSP2ClassGen.dbhandler d = new DBSP2ClassGen.dbhandler();
                d.BuildAllStoredProcedureInfo(txtIP.Text.ToString(), txtPort.Text.ToString(), txtUsername.Text.ToString(), txtPassword.Text.ToString(), cbxDatabaseList.Text.ToString());
                d.LoadAllTemplate();
                d.Build();
                
                
            }
            else
            {
                MessageBox.Show("You canceled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
        }
    }
    
}

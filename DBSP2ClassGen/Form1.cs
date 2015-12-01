using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace DBSP2ClassGen
{
    public partial class MainForm : Form
    {
        public string IniFileName;
        public string adHocFilename;
        public string AppDefaultDir; 
        public MainForm()
        {
            InitializeComponent();

            if( File.Exists("./config.json") )
            {
                DBSP2ClassGen.dbhandler d = new DBSP2ClassGen.dbhandler();
                ConfigInfo ci = new ConfigInfo();
                d.GetConfigInfo(ref ci);


                txtIP.Text = ci.IP.ToString();
                txtPort.Text = ci.port.ToString();
                txtUsername.Text = ci.Username.ToString();
                txtPassword.Text = ci.password.ToString();
                cbxDatabaseList.Text = ci.dbname.ToString();
                if( ci.adHocFilename != null )
                    lblAdHoc.Text = ci.adHocFilename.ToString();
                

            }

            

            AppDefaultDir = System.Environment.CurrentDirectory;
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
            openFileDialog1.Filter = "JSON file(*.json)|*.json";
            openFileDialog1.InitialDirectory = ".";
            openFileDialog1.FileName = "";            
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.IniFileName = openFileDialog1.FileName;
                lblAdHoc.Text = "Ad-hoc Query File : " + this.IniFileName;

                adHocFilename = new StringBuilder(openFileDialog1.FileName).ToString(); 
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
               cbxDatabaseList.Text.ToString().Length == 0 &&
               adHocFilename.Length == 0 ) 
            {
                MessageBox.Show("Fill the empty field(s).","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            saveFileDialog1.InitialDirectory = AppDefaultDir;
            if( saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String SaveFile = saveFileDialog1.FileName;

                DBSP2ClassGen.dbhandler d = new DBSP2ClassGen.dbhandler();
                d.BuildAllStoredProcedureInfo(txtIP.Text.ToString(), txtPort.Text.ToString(), txtUsername.Text.ToString(), txtPassword.Text.ToString(), cbxDatabaseList.Text.ToString());
                d.LoadAllTemplate();
                if( adHocFilename != null )
                { 
                    d.LoadAdHocQueryInfo(adHocFilename.ToString());
                }
                d.Build(saveFileDialog1.FileName.ToString());
                MessageBox.Show("File generated.", "info", MessageBoxButtons.OK);
                d.SaveConfig();
            }
            else
            {
                MessageBox.Show("You canceled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {            

        }
    }
    
}

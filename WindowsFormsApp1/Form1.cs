using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AxZKFPEngXControl;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private string conexion = "datasource=127.0.0.1;port=3306;username=root;password=;database=biomet";
        string[,] afiliados = new string[600, 10];
        int afiliadosLen;
        private AxZKFPEngX ZkFprint = new AxZKFPEngX();
        private bool Check;
        public Form1()
        {
            InitializeComponent();
            afiliadosGet();
        }
        public void afiliadosGet()
        {
            MySqlConnection conexionDB = new MySqlConnection(conexion);
            try
            {
                conexionDB.Open();
                MySqlCommand command = new MySqlCommand("SELECT *, CONCAT(apellidos, ' ', nombres) AS nombrecompleto FROM afiliados ORDER BY nombrecompleto;", conexionDB);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    afiliadosLen = 0;
                    while (reader.Read())
                    {
                        afiliados[afiliadosLen, 0] = reader.GetString(0);
                        afiliados[afiliadosLen, 1] = reader.GetString(11);
                        afiliados[afiliadosLen, 2] = reader.GetString(5);
                        afiliados[afiliadosLen, 3] = reader.GetString(6);
                        afiliados[afiliadosLen, 4] = reader.GetString(7);
                        afiliadosLen++;
                    }
                }
                conexionDB.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = null;
            comboBox1.Items.Clear();

            comboBox1.DisplayMember = "nombre";
            comboBox1.ValueMember = "id";
            MySqlConnection conexionDB = new MySqlConnection(conexion);
            conexionDB.Open();
            try
            {
                MySqlCommand comando = new MySqlCommand("SELECT *, CONCAT(apellidos, ' ', nombres) AS nombrecompleto FROM afiliados ORDER BY nombrecompleto;", conexionDB);
                MySqlDataAdapter data = new MySqlDataAdapter(comando);
                DataTable dt = new DataTable();
                data.Fill(dt);
                comboBox1.ValueMember = "id";
                comboBox1.DisplayMember = "nombrecompleto";
                comboBox1.DataSource = dt;
                conexionDB.Close();
            } catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }

            Controls.Add(ZkFprint);
            InitialAxZkfp();
            //MySqlConnection connection = new MySqlConnection("datasource=127.0.0.1;port=3306;username=root;password=;database=biometrico");
            //MySqlCommand command = new MySqlCommand("SELECT * FROM afiliados ORDER BY apellidos",connection);
            //try
            //{
            //connection.Open();
            //MySqlDataReader reader = command.ExecuteReader();
            //if (reader.HasRows)
            //{
            //while (reader.Read())
            //{
            //MessageBox.Show(reader.GetString(4));
            //comboBox1.Items.Add(new { nombre = reader.GetString(4) + ' '+reader.GetString(3), id = reader.GetString(0) });
            //}
            //}
            //connection.Close();
            //}catch(Exception error)
            //{
            //    MessageBox.Show(error.ToString());
            //}
        }
        private void InitialAxZkfp()
        {
            try
            {

                ZkFprint.OnImageReceived += zkFprint_OnImageReceived;
                ZkFprint.OnFeatureInfo += zkFprint_OnFeatureInfo;
                //zkFprint.OnFingerTouching 
                //zkFprint.OnFingerLeaving
                ZkFprint.OnEnroll += zkFprint_OnEnroll;

                if (ZkFprint.InitEngine() == 0)
                {
                    ZkFprint.FPEngineVersion = "9";
                    ZkFprint.EnrollCount = 3;
                    deviceSerial.Text += " " + ZkFprint.SensorSN + " Count: " + ZkFprint.SensorCount.ToString() + " Index: " + ZkFprint.SensorIndex.ToString();
                    ShowHintInfo("Device successfully connected");
                }

            }
            catch (Exception ex)
            {
                ShowHintInfo("Device init err, error: " + ex.Message);
            }
        }
        private void ShowHintInfo(String s)
        {
            prompt.Text = s;
        }
        private void zkFprint_OnImageReceived(object sender, IZKFPEngXEvents_OnImageReceivedEvent e)
        {
            Graphics g = fpicture.CreateGraphics();
            Bitmap bmp = new Bitmap(fpicture.Width, fpicture.Height);
            g = Graphics.FromImage(bmp);
            int dc = g.GetHdc().ToInt32();
            ZkFprint.PrintImageAt(dc, 0, 0, bmp.Width, bmp.Height);
            g.Dispose();
            fpicture.Image = bmp;
        }

        private void zkFprint_OnFeatureInfo(object sender, IZKFPEngXEvents_OnFeatureInfoEvent e)
        {

            String strTemp = string.Empty;
            if (ZkFprint.EnrollIndex != 1)
            {
                if (ZkFprint.IsRegister)
                {
                    if (ZkFprint.EnrollIndex - 1 > 0)
                    {
                        int eindex = ZkFprint.EnrollIndex - 1;
                        strTemp = "Please scan again ..." + eindex;
                    }
                }
            }
            ShowHintInfo(strTemp);
        }
        private void zkFprint_OnEnroll(object sender, IZKFPEngXEvents_OnEnrollEvent e)
        {
            if (e.actionResult)
            {


                string template = ZkFprint.EncodeTemplate1(e.aTemplate);
                txtTemplate.Text = template;
                ShowHintInfo("Registration successful. You can verify now");
                //btnRegister.Enabled = false;
                btnVerify.Enabled = true;
            }
            else
            {
                ShowHintInfo("Error, please register again.");

            }
        }
        private void zkFprint_OnCapture(object sender, IZKFPEngXEvents_OnCaptureEvent e)
        {
            string template = ZkFprint.EncodeTemplate1(e.aTemplate);

            for (int i = 0; i < afiliadosLen; i++)
            {
                if (ZkFprint.VerFingerFromStr(ref template, afiliados[i, 2], false, ref Check))
                {
                    ShowHintInfo(afiliados[i, 1]);
                    registerPagos(afiliados[i, 0]);
                    break;
                }
                else if (ZkFprint.VerFingerFromStr(ref template, afiliados[i, 3], false, ref Check))
                {
                    ShowHintInfo(afiliados[i, 1]);
                    registerPagos(afiliados[i, 0]);
                    break;
                }
                else if (ZkFprint.VerFingerFromStr(ref template, afiliados[i, 4], false, ref Check))
                {
                    ShowHintInfo(afiliados[i, 1]);
                    registerPagos(afiliados[i, 0]);
                    break;
                }
                else
                {
                    ShowHintInfo("Not Verified");
                    //break;
                }

            }

            //if (ZkFprint.VerFingerFromStr(ref template, txtTemplate.Text, false, ref Check))
            //{
            //    ShowHintInfo("Verified");
            //}
            //else
            //    ShowHintInfo("Not Verified");

        }
        private void registerPagos(string id){

            MySqlConnection conexionDB = new MySqlConnection(conexion);
            try
            {
                String vehiculo_id = "";
                String afiliado_id = "";
                String grupo_id = "";
                String monto = "0";
                conexionDB.Open();
                MySqlCommand command = new MySqlCommand("SELECT v.id,afiliado_id,grupo_id,monto FROM vehiculos v INNER JOIN grupos g ON v.grupo_id=g.id WHERE afiliado_id="+id+" LIMIT 1", conexionDB);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {

                    while (reader.Read())
                    {
                        vehiculo_id= reader.GetString(0);
                        afiliado_id = reader.GetString(1);
                        grupo_id = reader.GetString(2);
                        monto = reader.GetString(3);
                    }
                    

                }
                conexionDB.Close();
                conexionDB.Open();
                DateTime aDate = DateTime.Now;
                string fecha = aDate.ToString("yyyy-MM-dd").ToString();
                MySqlCommand command2 = new MySqlCommand("INSERT INTO pagos SET afiliado_id=" + afiliado_id + ", grupo_id=" + grupo_id + ", vehiculo_id=" + vehiculo_id + ",monto=" + monto + ",fecha='"+fecha+"'", conexionDB);
                MySqlDataReader reader2 = command2.ExecuteReader();
                conexionDB.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            ZkFprint.CancelEnroll();
            ZkFprint.EnrollCount = 3;
            ZkFprint.BeginEnroll();
            ShowHintInfo("Please give fingerprint sample.");
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            if (ZkFprint.IsRegister)
            {
                ZkFprint.CancelEnroll();
            }
            ZkFprint.OnCapture += zkFprint_OnCapture;
            ZkFprint.BeginCapture();
            ShowHintInfo("Please give fingerprint sample.");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            MySqlConnection conexionDB = new MySqlConnection(conexion);
            try
            {
                conexionDB.Open();
                MySqlCommand command = new MySqlCommand("SELECT * FROM afiliados WHERE id =" + comboBox1.SelectedValue.ToString(), conexionDB);
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        textBox1.Text = reader.GetString(5);
                        textBox2.Text = reader.GetString(6);
                        textBox3.Text = reader.GetString(7);
                    }
                }
                conexionDB.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }

            //int selectedIndex = comboBox1.SelectedValue.ToString();
            //comboBox1.SelectedItem.ToString();
            //MessageBox.Show(comboBox1.SelectedValue.ToString());
            //var o =new { id=1 };
            //o = comboBox1.SelectedItem;
            //Console.WriteLine(o.id);
            //MessageBox.Show("Selected Type : " + comboBox1.SelectedIndex.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MySqlConnection conexionDB = new MySqlConnection(conexion);
            try
            {
                conexionDB.Open();
                MySqlCommand command = new MySqlCommand("UPDATE afiliados SET dedo1='"+textBox1.Text+ "',dedo2='" + textBox2.Text + "',dedo3='" + textBox3.Text + "' WHERE id =" + comboBox1.SelectedValue.ToString(), conexionDB);
                MySqlDataReader reader = command.ExecuteReader();
                conexionDB.Close();
                afiliadosGet();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = txtTemplate.Text;
            //txtTemplate.Text = "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = txtTemplate.Text;
            //txtTemplate.Text = "";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox3.Text = txtTemplate.Text;
            //txtTemplate.Text = "";
        }
    }
}

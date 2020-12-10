using System;
using System.Windows.Forms;

namespace KnowledgeBaseToCSV
{
    public partial class Form1 : Form
    {
        public int pageSize; 
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Only numbers accepted as input");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool isNumeric = int.TryParse(textBox1.Text, out int n);
            if (isNumeric)
            {
                pageSize = n;
            }
            else
            {
                throw new Exception("Only numbers accepted as input");
            }
            this.Close();
        }
    }
}
